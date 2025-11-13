CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Simulations" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Simulations" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "StartTime" datetime2 NOT NULL,
    "FileName" TEXT NOT NULL,
    "Status" TEXT NOT NULL DEFAULT 'NotStarted',
    CONSTRAINT "CK_Simulations_Status" CHECK (Status IN ('NotStarted', 'InProgress', 'Completed'))
);

CREATE INDEX "IX_Simulations_FileName_Status" ON "Simulations" ("FileName", "Status");

CREATE INDEX "IX_Simulations_StartTime" ON "Simulations" ("StartTime");

CREATE INDEX "IX_Simulations_Status" ON "Simulations" ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251111035138_InitialCreate', '9.0.0');

ALTER TABLE "Simulations" ADD "RowVersion" BLOB NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251112190858_AddSimulationRowVersion', '9.0.0');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251112191110_AddSimulationRowVersionColumn', '9.0.0');

CREATE TABLE "AuditEntries" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AuditEntries" PRIMARY KEY AUTOINCREMENT,
    "SimulationId" INTEGER NOT NULL,
    "Actor" TEXT NOT NULL,
    "CorrelationId" TEXT NULL,
    "TimestampUtc" TEXT NOT NULL,
    "ChangesJson" TEXT NOT NULL,
    "PrevETag" TEXT NULL,
    "NewETag" TEXT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251113034107_CreateAuditEntries', '9.0.0');

COMMIT;

