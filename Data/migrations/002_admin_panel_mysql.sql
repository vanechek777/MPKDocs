-- Админ-панель: флаг администратора, категории шаблонов, журнал действий.
-- Выполните один раз на существующей БД: mysql ... < 002_admin_panel_mysql.sql

USE `MPKDocuments`;

-- Пользователи: администратор в БД (дополнительно к MPK_ADMIN_USER_IDS)
ALTER TABLE `Users` ADD COLUMN `IsAdmin` TINYINT(1) NOT NULL DEFAULT 0;
UPDATE `Users` SET `IsAdmin` = 1 WHERE `PhoneNumber` = '+79148012594';

CREATE TABLE `DocumentCategories` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255) NOT NULL,
  `SortOrder` INT NOT NULL DEFAULT 0,
  `isActive` TINYINT(1) NULL DEFAULT 1,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

ALTER TABLE `DocumentTemplates`
  ADD COLUMN `CategoryId` INT NULL,
  ADD KEY `IX_DocumentTemplates_CategoryId` (`CategoryId`),
  ADD CONSTRAINT `FK_DocumentTemplates_CategoryId`
    FOREIGN KEY (`CategoryId`) REFERENCES `DocumentCategories` (`id`)
    ON DELETE SET NULL ON UPDATE CASCADE;

CREATE TABLE `UserActivityLogs` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `UserId` INT NULL,
  `Action` VARCHAR(80) NOT NULL,
  `Detail` LONGTEXT NULL,
  `CreatedAt` DATETIME(6) NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (`id`),
  KEY `IX_UserActivityLogs_UserId` (`UserId`),
  CONSTRAINT `FK_UserActivityLogs_UserId`
    FOREIGN KEY (`UserId`) REFERENCES `Users` (`id`)
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO `DocumentCategories` (`Name`, `SortOrder`, `isActive`) VALUES
  ('Отчёты', 10, 1),
  ('Приказы', 20, 1),
  ('Прочее', 90, 1);
