-- SQL migration script for AddIsDeletedAndAuditAction
-- Generated: 2025-11-13
-- Targets SQLite: adds IsDeleted to Simulations and Action to AuditEntries

BEGIN TRANSACTION;

-- Add IsDeleted column to Simulations (NOT NULL with default 0)
ALTER TABLE "Simulations" ADD COLUMN "IsDeleted" INTEGER NOT NULL DEFAULT 0;

-- Add Action column to AuditEntries (nullable text)
ALTER TABLE "AuditEntries" ADD COLUMN "Action" TEXT;

COMMIT;
