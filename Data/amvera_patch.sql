-- Догоняющие миграции для БД, созданной ранним amvera_init.sql.
-- Выполните один раз в phpMyAdmin на базе MPKDocuments.

USE `MPKDocuments`;

-- 003: Email
SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'Email'
);
SET @sql := IF(
  @col_exists = 0,
  'ALTER TABLE `Users` ADD COLUMN `Email` VARCHAR(255) NULL AFTER `PhoneNumber`',
  'SELECT ''Users.Email already exists'''
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 004: DocumentUserViews
CREATE TABLE IF NOT EXISTS `DocumentUserViews` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `DocumentId` INT NOT NULL,
  `UserId` INT NOT NULL,
  `FirstViewedAt` DATETIME(6) NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_DocumentUserViews_Doc_User` (`DocumentId`, `UserId`),
  KEY `IX_DocumentUserViews_UserId` (`UserId`),
  CONSTRAINT `FK_DocumentUserViews_Document`
    FOREIGN KEY (`DocumentId`) REFERENCES `Documents` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_DocumentUserViews_User`
    FOREIGN KEY (`UserId`) REFERENCES `Users` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 005: DocumentHashHex
SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'DigitalSignatures' AND COLUMN_NAME = 'DocumentHashHex'
);
SET @sql := IF(
  @col_exists = 0,
  'ALTER TABLE `DigitalSignatures` ADD COLUMN `DocumentHashHex` VARCHAR(128) NULL AFTER `SignatureHex`',
  'SELECT ''DigitalSignatures.DocumentHashHex already exists'''
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
