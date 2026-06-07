-- Колонка Email для OTP/входа по почте (если БД создана до появления поля в модели).
USE `MPKDocuments`;

ALTER TABLE `Users`
  ADD COLUMN `Email` VARCHAR(255) NULL AFTER `PhoneNumber`;
