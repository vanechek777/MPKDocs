-- Хеш документа на момент НЭП-подписи.
USE `MPKDocuments`;

ALTER TABLE `DigitalSignatures`
  ADD COLUMN `DocumentHashHex` VARCHAR(128) NULL AFTER `SignatureHex`;
