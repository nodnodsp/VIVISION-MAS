PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    user_code TEXT NOT NULL UNIQUE,
    user_name TEXT NOT NULL,
    role_code TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS instruments (
    id TEXT PRIMARY KEY,
    instrument_code TEXT NOT NULL UNIQUE,
    instrument_name TEXT NOT NULL,
    model TEXT NOT NULL,
    serial_number TEXT,
    firmware_version TEXT,
    connection_type TEXT NOT NULL,
    port_name TEXT,
    baud_rate INTEGER,
    driver_status TEXT,
    last_connected_at TEXT,
    is_default INTEGER NOT NULL DEFAULT 0,
    status TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS calibration_records (
    id TEXT PRIMARY KEY,
    instrument_id TEXT NOT NULL,
    calibration_type TEXT NOT NULL,
    result_code TEXT NOT NULL,
    error_code TEXT,
    error_message TEXT,
    operator_id TEXT,
    started_at TEXT NOT NULL,
    finished_at TEXT,
    expires_at TEXT,
    remark TEXT,
    FOREIGN KEY (instrument_id) REFERENCES instruments(id)
);

CREATE TABLE IF NOT EXISTS color_libraries (
    id TEXT PRIMARY KEY,
    library_code TEXT NOT NULL UNIQUE,
    library_name TEXT NOT NULL,
    category_name TEXT,
    description TEXT,
    is_default INTEGER NOT NULL DEFAULT 0,
    created_by TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS tolerance_templates (
    id TEXT PRIMARY KEY,
    template_code TEXT NOT NULL UNIQUE,
    template_name TEXT NOT NULL,
    product_type TEXT,
    delta_e_formula TEXT NOT NULL,
    overall_lower_limit REAL,
    overall_upper_limit REAL,
    effect_lower_limit REAL,
    effect_upper_limit REAL,
    warning_enabled INTEGER NOT NULL DEFAULT 1,
    is_default INTEGER NOT NULL DEFAULT 0,
    status TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS samples (
    id TEXT PRIMARY KEY,
    sample_code TEXT NOT NULL,
    sample_name TEXT NOT NULL,
    batch_no TEXT,
    material_name TEXT,
    color_name TEXT,
    source_type TEXT,
    status TEXT NOT NULL,
    remark TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS standard_samples (
    id TEXT PRIMARY KEY,
    library_id TEXT NOT NULL,
    standard_code TEXT NOT NULL,
    standard_name TEXT NOT NULL,
    version_no INTEGER NOT NULL,
    material_name TEXT,
    color_name TEXT,
    batch_no TEXT,
    tolerance_template_id TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    is_default_version INTEGER NOT NULL DEFAULT 1,
    remark TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (library_id) REFERENCES color_libraries(id),
    FOREIGN KEY (tolerance_template_id) REFERENCES tolerance_templates(id)
);

CREATE TABLE IF NOT EXISTS tolerance_angle_rules (
    id TEXT PRIMARY KEY,
    template_id TEXT NOT NULL,
    angle_code TEXT NOT NULL,
    metric_code TEXT NOT NULL,
    lower_limit REAL,
    upper_limit REAL,
    warning_lower_limit REAL,
    warning_upper_limit REAL,
    is_enabled INTEGER NOT NULL DEFAULT 1,
    sort_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (template_id) REFERENCES tolerance_templates(id)
);

CREATE TABLE IF NOT EXISTS measurement_tasks (
    id TEXT PRIMARY KEY,
    task_code TEXT NOT NULL UNIQUE,
    instrument_id TEXT NOT NULL,
    sample_id TEXT,
    standard_sample_id TEXT,
    template_id TEXT,
    task_type TEXT NOT NULL,
    measurement_mode TEXT NOT NULL,
    average_count INTEGER,
    interval_seconds INTEGER,
    status TEXT NOT NULL,
    created_by TEXT,
    started_at TEXT,
    finished_at TEXT,
    remark TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (instrument_id) REFERENCES instruments(id),
    FOREIGN KEY (sample_id) REFERENCES samples(id),
    FOREIGN KEY (standard_sample_id) REFERENCES standard_samples(id),
    FOREIGN KEY (template_id) REFERENCES tolerance_templates(id)
);

CREATE TABLE IF NOT EXISTS measurement_records (
    id TEXT PRIMARY KEY,
    task_id TEXT NOT NULL,
    record_no INTEGER NOT NULL,
    record_type TEXT NOT NULL,
    total_delta_e REAL,
    total_effect_diff REAL,
    pass_status TEXT NOT NULL,
    result_summary TEXT,
    measurement_snapshot_json TEXT,
    measured_at TEXT NOT NULL,
    created_at TEXT NOT NULL,
    FOREIGN KEY (task_id) REFERENCES measurement_tasks(id)
);

CREATE TABLE IF NOT EXISTS measurement_angle_results (
    id TEXT PRIMARY KEY,
    record_id TEXT NOT NULL,
    angle_code TEXT NOT NULL,
    cie_l REAL,
    cie_a REAL,
    cie_b REAL,
    cie_c REAL,
    cie_h REAL,
    cie_x REAL,
    cie_y REAL,
    cie_z REAL,
    delta_l REAL,
    delta_a REAL,
    delta_b REAL,
    delta_c REAL,
    delta_h REAL,
    delta_e REAL,
    pass_status TEXT NOT NULL,
    raw_value_json TEXT,
    FOREIGN KEY (record_id) REFERENCES measurement_records(id)
);

CREATE TABLE IF NOT EXISTS measurement_effect_results (
    id TEXT PRIMARY KEY,
    record_id TEXT NOT NULL,
    angle_code TEXT,
    sparkle_value REAL,
    sparkle_diff REAL,
    graininess_value REAL,
    graininess_diff REAL,
    effect_pass_status TEXT NOT NULL,
    raw_effect_json TEXT,
    FOREIGN KEY (record_id) REFERENCES measurement_records(id)
);

CREATE TABLE IF NOT EXISTS report_exports (
    id TEXT PRIMARY KEY,
    record_id TEXT NOT NULL,
    report_code TEXT NOT NULL,
    template_name TEXT,
    file_format TEXT NOT NULL,
    file_path TEXT,
    exported_by TEXT,
    exported_at TEXT NOT NULL,
    export_status TEXT NOT NULL,
    remark TEXT,
    FOREIGN KEY (record_id) REFERENCES measurement_records(id)
);

CREATE TABLE IF NOT EXISTS operation_logs (
    id TEXT PRIMARY KEY,
    task_id TEXT,
    record_id TEXT,
    operator_id TEXT,
    module_name TEXT NOT NULL,
    operation_type TEXT NOT NULL,
    operation_desc TEXT,
    operation_result TEXT NOT NULL,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS raw_packets (
    id TEXT PRIMARY KEY,
    task_id TEXT,
    instrument_id TEXT,
    direction TEXT NOT NULL,
    packet_type TEXT,
    packet_hex TEXT,
    packet_text TEXT,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS system_settings (
    id TEXT PRIMARY KEY,
    setting_key TEXT NOT NULL UNIQUE,
    setting_value TEXT,
    value_type TEXT,
    updated_at TEXT NOT NULL,
    updated_by TEXT
);
