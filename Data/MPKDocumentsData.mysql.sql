-- MySQL 8.x schema for MPKDocuments (converted from SQL Server)
-- Engine: InnoDB, charset: utf8mb4

SET NAMES utf8mb4;
SET time_zone = '+00:00';

DROP DATABASE IF EXISTS `MPKDocuments`;
CREATE DATABASE `MPKDocuments`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

USE `MPKDocuments`;

-- ----------------------------
-- Tables
-- ----------------------------

CREATE TABLE `Departments` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `OneCId` VARCHAR(50) NULL,
  `Name` VARCHAR(255) NOT NULL,
  `ParentDepartmentId` INT NULL,
  `isActive` TINYINT(1) NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  CONSTRAINT `FK_Departments_ParentDepartmentId`
    FOREIGN KEY (`ParentDepartmentId`) REFERENCES `Departments` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `Positions` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `OneCId` VARCHAR(50) NULL,
  `Name` VARCHAR(255) NOT NULL,
  `isActive` TINYINT(1) NULL DEFAULT 1,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `Users` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `OneCId` VARCHAR(50) NULL,
  `PhoneNumber` VARCHAR(20) NOT NULL,
  `FullName` VARCHAR(255) NOT NULL,
  `PositionId` INT NULL,
  `DepartmentId` INT NULL,
  `Status` TINYINT(1) NULL DEFAULT 1,
  `PasswordHash` LONGTEXT NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Users_PhoneNumber` (`PhoneNumber`),
  KEY `IX_Users_PositionId` (`PositionId`),
  KEY `IX_Users_DepartmentId` (`DepartmentId`),
  CONSTRAINT `FK_Users_DepartmentId`
    FOREIGN KEY (`DepartmentId`) REFERENCES `Departments` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `FK_Users_PositionId`
    FOREIGN KEY (`PositionId`) REFERENCES `Positions` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `DocumentTemplates` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255) NOT NULL,
  `TemplatePath` LONGTEXT NOT NULL,
  `FormSchema` JSON NOT NULL,
  `isActive` TINYINT(1) NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  CONSTRAINT `CHK_FormSchema_JSON` CHECK (JSON_VALID(`FormSchema`))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `Documents` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `TemplateId` INT NOT NULL,
  `InitiatorId` INT NOT NULL,
  `Status` VARCHAR(50) NULL DEFAULT 'DRAFT',
  `FinalDocumentHash` VARCHAR(256) NULL,
  `FinalPDFPath` LONGTEXT NULL,
  `CreatedAt` DATETIME(6) NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (`id`),
  KEY `IX_Documents_TemplateId` (`TemplateId`),
  KEY `IX_Documents_InitiatorId` (`InitiatorId`),
  CONSTRAINT `FK_Documents_TemplateId`
    FOREIGN KEY (`TemplateId`) REFERENCES `DocumentTemplates` (`id`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT `FK_Documents_InitiatorId`
    FOREIGN KEY (`InitiatorId`) REFERENCES `Users` (`id`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `DocumentContents` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `DocumentId` INT NOT NULL,
  `DataJson` JSON NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_DocumentContents_DocumentId` (`DocumentId`),
  CONSTRAINT `FK_DocumentContents_DocumentId`
    FOREIGN KEY (`DocumentId`) REFERENCES `Documents` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `CHK_DataJson_JSON` CHECK (JSON_VALID(`DataJson`))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `DocumentSteps` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `DocumentId` INT NOT NULL,
  `StepOrder` INT NOT NULL,
  `ApprovalMode` VARCHAR(50) NOT NULL,
  `Status` VARCHAR(50) NULL DEFAULT 'PENDING',
  PRIMARY KEY (`id`),
  KEY `IX_DocumentSteps_DocumentId` (`DocumentId`),
  CONSTRAINT `FK_DocumentSteps_DocumentId`
    FOREIGN KEY (`DocumentId`) REFERENCES `Documents` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `DocumentTasks` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `StepId` INT NOT NULL,
  `DocumentId` INT NOT NULL,
  `AssignedUserId` INT NULL,
  `AssignedPositionId` INT NULL,
  `AssignedDepartmentId` INT NULL,
  `Status` VARCHAR(50) NULL DEFAULT 'PENDING',
  `ProcessedByUserId` INT NULL,
  `ProcessedAt` DATETIME(6) NULL,
  PRIMARY KEY (`id`),
  KEY `IX_DocumentTasks_StepId` (`StepId`),
  KEY `IX_DocumentTasks_DocumentId` (`DocumentId`),
  KEY `IX_DocumentTasks_AssignedUserId` (`AssignedUserId`),
  KEY `IX_DocumentTasks_AssignedPositionId` (`AssignedPositionId`),
  KEY `IX_DocumentTasks_AssignedDepartmentId` (`AssignedDepartmentId`),
  KEY `IX_DocumentTasks_ProcessedByUserId` (`ProcessedByUserId`),
  CONSTRAINT `FK_DocumentTasks_StepId`
    FOREIGN KEY (`StepId`) REFERENCES `DocumentSteps` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_DocumentTasks_DocumentId`
    FOREIGN KEY (`DocumentId`) REFERENCES `Documents` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_DocumentTasks_AssignedUserId`
    FOREIGN KEY (`AssignedUserId`) REFERENCES `Users` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `FK_DocumentTasks_AssignedPositionId`
    FOREIGN KEY (`AssignedPositionId`) REFERENCES `Positions` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `FK_DocumentTasks_AssignedDepartmentId`
    FOREIGN KEY (`AssignedDepartmentId`) REFERENCES `Departments` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `FK_DocumentTasks_ProcessedByUserId`
    FOREIGN KEY (`ProcessedByUserId`) REFERENCES `Users` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `SignatureProfiles` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `UserId` INT NOT NULL,
  `PublicKey` LONGTEXT NOT NULL,
  `EncryptedPrivateKey` LONGTEXT NOT NULL,
  `CreatedAt` DATETIME(6) NULL DEFAULT (UTC_TIMESTAMP(6)),
  `isRevoked` TINYINT(1) NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `IX_SignatureProfiles_UserId` (`UserId`),
  CONSTRAINT `FK_SignatureProfiles_UserId`
    FOREIGN KEY (`UserId`) REFERENCES `Users` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `DigitalSignatures` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `DocumentId` INT NOT NULL,
  `UserId` INT NOT NULL,
  `SignatureHex` LONGTEXT NOT NULL,
  `SignedAt` DATETIME(6) NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (`id`),
  KEY `IX_DigitalSignatures_DocumentId` (`DocumentId`),
  KEY `IX_DigitalSignatures_UserId` (`UserId`),
  CONSTRAINT `FK_DigitalSignatures_DocumentId`
    FOREIGN KEY (`DocumentId`) REFERENCES `Documents` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_DigitalSignatures_UserId`
    FOREIGN KEY (`UserId`) REFERENCES `Users` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `WorkflowTemplates` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `TemplateId` INT NOT NULL,
  `StepOrder` INT NOT NULL,
  `TargetUserId` INT NULL,
  `TargetPositionId` INT NULL,
  `TargetDepartmentId` INT NULL,
  `ApprovalMode` VARCHAR(50) NULL DEFAULT 'ANY',
  `ActionType` VARCHAR(50) NULL DEFAULT 'SIGN',
  PRIMARY KEY (`id`),
  KEY `IX_WorkflowTemplates_TemplateId` (`TemplateId`),
  KEY `IX_WorkflowTemplates_TargetUserId` (`TargetUserId`),
  KEY `IX_WorkflowTemplates_TargetPositionId` (`TargetPositionId`),
  KEY `IX_WorkflowTemplates_TargetDepartmentId` (`TargetDepartmentId`),
  CONSTRAINT `FK_WorkflowTemplates_TemplateId`
    FOREIGN KEY (`TemplateId`) REFERENCES `DocumentTemplates` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_WorkflowTemplates_TargetUserId`
    FOREIGN KEY (`TargetUserId`) REFERENCES `Users` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `FK_WorkflowTemplates_TargetPositionId`
    FOREIGN KEY (`TargetPositionId`) REFERENCES `Positions` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `FK_WorkflowTemplates_TargetDepartmentId`
    FOREIGN KEY (`TargetDepartmentId`) REFERENCES `Departments` (`id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
  -- NOTE: MySQL can reject CHECK that references columns participating in FK with referential actions.
  -- Enforce the same rule via triggers below.
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

DELIMITER $$

CREATE TRIGGER `TRG_WorkflowTemplates_ValidateTarget_BI`
BEFORE INSERT ON `WorkflowTemplates`
FOR EACH ROW
BEGIN
  IF NEW.`TargetUserId` IS NULL AND NEW.`TargetPositionId` IS NULL AND NEW.`TargetDepartmentId` IS NULL THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'WorkflowTemplates: one of TargetUserId/TargetPositionId/TargetDepartmentId must be set';
  END IF;
END$$

CREATE TRIGGER `TRG_WorkflowTemplates_ValidateTarget_BU`
BEFORE UPDATE ON `WorkflowTemplates`
FOR EACH ROW
BEGIN
  IF NEW.`TargetUserId` IS NULL AND NEW.`TargetPositionId` IS NULL AND NEW.`TargetDepartmentId` IS NULL THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'WorkflowTemplates: one of TargetUserId/TargetPositionId/TargetDepartmentId must be set';
  END IF;
END$$

DELIMITER ;

