BEGIN TRANSACTION;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251114003358_AddIsDeletedAndAuditAction', '9.0.0');

ALTER TABLE "Simulations" ADD "IsDeleted" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "AuditEntries" ADD "Action" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251114003807_AddIsDeletedAndAuditAction_EF', '9.0.0');

COMMIT;

