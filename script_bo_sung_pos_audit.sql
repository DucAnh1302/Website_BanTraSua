USE db_trasua;
GO

-- 1. Bang quan ly ca lam viec & ket tien thu ngan POS (pos_shifts)
IF OBJECT_ID('dbo.pos_shifts', 'U') IS NULL
BEGIN
    CREATE TABLE [pos_shifts] (
      [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
      [shift_code] NVARCHAR(50) NOT NULL UNIQUE,
      [staff_id] BIGINT NOT NULL,
      [opening_cash] DECIMAL(12,0) NOT NULL DEFAULT 0,
      [cash_sales] DECIMAL(12,0) NOT NULL DEFAULT 0,
      [transfer_sales] DECIMAL(12,0) NOT NULL DEFAULT 0,
      [expected_cash] DECIMAL(12,0) NOT NULL DEFAULT 0,
      [actual_cash] DECIMAL(12,0) NULL DEFAULT NULL,
      [difference] DECIMAL(12,0) NULL DEFAULT NULL,
      [status] NVARCHAR(20) NOT NULL DEFAULT 'open',
      [opened_at] DATETIME NOT NULL DEFAULT GETDATE(),
      [closed_at] DATETIME NULL DEFAULT NULL,
      [notes] NVARCHAR(MAX) NULL DEFAULT NULL,
      [created_at] DATETIME NULL DEFAULT GETDATE(),
      [updated_at] DATETIME NULL DEFAULT GETDATE()
    );

    ALTER TABLE [pos_shifts]
      ADD CONSTRAINT [fk_pos_shifts_staff_id] FOREIGN KEY ([staff_id]) REFERENCES [users] ([id]);
END
GO

-- 2. Bang nhat ky kiem toan he thong (audit_logs)
IF OBJECT_ID('dbo.audit_logs', 'U') IS NULL
BEGIN
    CREATE TABLE [audit_logs] (
      [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
      [user_id] BIGINT NULL DEFAULT NULL,
      [user_email] NVARCHAR(255) NULL DEFAULT NULL,
      [user_role] NVARCHAR(50) NULL DEFAULT NULL,
      [action] NVARCHAR(255) NOT NULL,
      [description] NVARCHAR(MAX) NULL DEFAULT NULL,
      [target_type] NVARCHAR(255) NULL DEFAULT NULL,
      [target_id] NVARCHAR(255) NULL DEFAULT NULL,
      [old_values] NVARCHAR(MAX) NULL DEFAULT NULL,
      [new_values] NVARCHAR(MAX) NULL DEFAULT NULL,
      [ip_address] NVARCHAR(45) NULL DEFAULT NULL,
      [user_agent] NVARCHAR(MAX) NULL DEFAULT NULL,
      [created_at] DATETIME NULL DEFAULT GETDATE(),
      [updated_at] DATETIME NULL DEFAULT GETDATE()
    );

    ALTER TABLE [audit_logs]
      ADD CONSTRAINT [fk_audit_logs_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]);
END
GO

-- 3. Dat lai mat khau chu thuong 'password' cho 4 tai khoan mau (dong bo che do chua bam mat khau)
UPDATE [users]
SET [password] = N'password', [status] = 1, [updated_at] = GETDATE()
WHERE [email] IN ('admin@trasua.test', 'pos@trasua.test', 'kho@trasua.test', 'customer@trasua.test');
GO
