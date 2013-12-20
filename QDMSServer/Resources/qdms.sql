CREATE DATABASE  IF NOT EXISTS `qdms` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `qdms`;
-- MySQL dump 10.13  Distrib 5.6.13, for Win32 (x86)
--
-- Host: 127.0.0.1    Database: qdms
-- ------------------------------------------------------
-- Server version	5.6.14-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `continuousfutures`
--

DROP TABLE IF EXISTS `continuousfutures`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `continuousfutures` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `InstrumentID` int(11) NOT NULL,
  `UnderlyingSymbolID` int(11) NOT NULL,
  `Month` int(11) NOT NULL,
  `RolloverType` tinyint(4) NOT NULL,
  `RolloverDays` int(11) NOT NULL,
  `AdjustmentMode` tinyint(4) NOT NULL,
  `UseJan` tinyint(1) NOT NULL,
  `UseFeb` tinyint(1) NOT NULL,
  `UseMar` tinyint(1) NOT NULL,
  `UseApr` tinyint(1) NOT NULL,
  `UseMay` tinyint(1) NOT NULL,
  `UseJun` tinyint(1) NOT NULL,
  `UseJul` tinyint(1) NOT NULL,
  `UseAug` tinyint(1) NOT NULL,
  `UseSep` tinyint(1) NOT NULL,
  `UseOct` tinyint(1) NOT NULL,
  `UseNov` tinyint(1) NOT NULL,
  `UseDec` tinyint(1) NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `FK_continuousfutures_instruments_ID` (`InstrumentID`),
  KEY `FK_continuousfutures_underlyingsymbols_ID` (`UnderlyingSymbolID`),
  CONSTRAINT `FK_continuousfutures_instruments_ID` FOREIGN KEY (`InstrumentID`) REFERENCES `instruments` (`ID`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `FK_continuousfutures_underlyingsymbols_ID` FOREIGN KEY (`UnderlyingSymbolID`) REFERENCES `underlyingsymbols` (`ID`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `continuousfutures`
--

LOCK TABLES `continuousfutures` WRITE;
/*!40000 ALTER TABLE `continuousfutures` DISABLE KEYS */;
/*!40000 ALTER TABLE `continuousfutures` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `datasources`
--

DROP TABLE IF EXISTS `datasources`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `datasources` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `NameIDX` (`Name`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `datasources`
--

LOCK TABLES `datasources` WRITE;
/*!40000 ALTER TABLE `datasources` DISABLE KEYS */;
INSERT INTO `datasources` VALUES (1,'Interactive Brokers'),(2,'Yahoo'),(3,'Quandl');
/*!40000 ALTER TABLE `datasources` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `exchanges`
--

DROP TABLE IF EXISTS `exchanges`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `exchanges` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL,
  `LongName` varchar(50) DEFAULT NULL,
  `Timezone` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UK_exchanges_name` (`Name`)
) ENGINE=InnoDB AUTO_INCREMENT=395 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `exchanges`
--

LOCK TABLES `exchanges` WRITE;
/*!40000 ALTER TABLE `exchanges` DISABLE KEYS */;
INSERT INTO `exchanges` VALUES (2,'AB','',NULL),(3,'AEB',NULL,NULL),(4,'ALSE',NULL,NULL),(5,'AMEX',NULL,NULL),(6,'AMS',NULL,NULL),(7,'ANTSE',NULL,NULL),(8,'AO',NULL,NULL),(9,'API',NULL,NULL),(10,'ARCA',NULL,NULL),(11,'ARCX',NULL,NULL),(12,'ASE',NULL,NULL),(13,'ASX',NULL,NULL),(14,'ASXI',NULL,NULL),(15,'ATA',NULL,NULL),(16,'ATAASE',NULL,NULL),(17,'ATH',NULL,NULL),(18,'ATHI',NULL,NULL),(19,'AUSSE',NULL,NULL),(20,'B',NULL,NULL),(21,'BARB',NULL,NULL),(22,'BARSE',NULL,NULL),(23,'BASB',NULL,NULL),(24,'BASE',NULL,NULL),(25,'BASLE',NULL,NULL),(26,'BB',NULL,NULL),(27,'BELFOX',NULL,NULL),(28,'BER',NULL,NULL),(29,'BERNB',NULL,NULL),(30,'BERSE',NULL,NULL),(31,'BET',NULL,NULL),(32,'BFE',NULL,NULL),(33,'BMBSE',NULL,NULL),(34,'BMF',NULL,NULL),(35,'BO',NULL,NULL),(36,'BOGSE',NULL,NULL),(37,'BOX',NULL,NULL),(38,'BRN',NULL,NULL),(39,'BRU',NULL,NULL),(40,'BRUSE',NULL,NULL),(41,'BRUT',NULL,NULL),(42,'BSE',NULL,NULL),(43,'BSP',NULL,NULL),(44,'BSPI',NULL,NULL),(45,'BT',NULL,NULL),(46,'BTRADE',NULL,NULL),(47,'BUD',NULL,NULL),(48,'BUDI',NULL,NULL),(49,'BUE',NULL,NULL),(50,'BUEI',NULL,NULL),(51,'BUTLR',NULL,NULL),(52,'BVME',NULL,NULL),(53,'C',NULL,NULL),(54,'CAES',NULL,NULL),(55,'CALC',NULL,NULL),(56,'CANX',NULL,NULL),(57,'CARSE',NULL,NULL),(58,'CBFX','test',NULL),(59,'CBOE','Chicago Board Options Exchange','Central Standard Time'),(60,'CBOT','Chicago Board of Trade','Central Standard Time'),(61,'CBT',NULL,NULL),(62,'CDE',NULL,NULL),(63,'CEC',NULL,NULL),(64,'CEX',NULL,NULL),(65,'CF',NULL,NULL),(66,'CFE','CBOE Futures Exchange','Central Standard Time'),(67,'CFFE',NULL,NULL),(68,'CFOREX',NULL,NULL),(69,'CFXT',NULL,NULL),(70,'CME','Chicago Mercantile Exchange','Central Standard Time'),(71,'CO',NULL,NULL),(72,'COATS',NULL,NULL),(73,'COLSE',NULL,NULL),(74,'COMEX',NULL,NULL),(75,'COMP',NULL,NULL),(76,'COMX',NULL,NULL),(77,'COPSE',NULL,NULL),(78,'CPC',NULL,NULL),(79,'CSC',NULL,NULL),(80,'CSE',NULL,NULL),(81,'CSEI',NULL,NULL),(82,'D',NULL,NULL),(83,'DBN',NULL,NULL),(84,'DBX',NULL,NULL),(85,'DBXI',NULL,NULL),(86,'DSE',NULL,NULL),(87,'DT',NULL,NULL),(88,'DTB',NULL,NULL),(89,'DUB',NULL,NULL),(90,'DUS',NULL,NULL),(91,'EBS',NULL,NULL),(92,'EBSBW',NULL,NULL),(93,'EBSSTK',NULL,NULL),(94,'EC',NULL,NULL),(95,'ECBOT',NULL,NULL),(96,'EEB',NULL,NULL),(97,'EEX',NULL,NULL),(98,'EIBI',NULL,NULL),(99,'EM',NULL,NULL),(100,'EOE',NULL,NULL),(101,'ESE',NULL,NULL),(102,'EUREX',NULL,NULL),(103,'EUREXUS',NULL,NULL),(104,'EUS',NULL,NULL),(105,'EUX',NULL,NULL),(106,'FOREX',NULL,NULL),(107,'FOX',NULL,NULL),(108,'FRA',NULL,NULL),(109,'FTA',NULL,NULL),(110,'FTSA',NULL,NULL),(111,'FTSE',NULL,NULL),(112,'FTSJ',NULL,NULL),(113,'FUKSE',NULL,NULL),(114,'FWB',NULL,NULL),(115,'FX',NULL,NULL),(116,'GARVIN',NULL,NULL),(117,'GB',NULL,NULL),(118,'GENEVA',NULL,NULL),(119,'GENEVB',NULL,NULL),(120,'GLOBEX','Chicago Mercantile Exchange (CME GLOBEX)','Central Standard Time'),(121,'HAM',NULL,NULL),(122,'HAN',NULL,NULL),(123,'HEL',NULL,NULL),(124,'HELI',NULL,NULL),(125,'HELSE',NULL,NULL),(126,'HIRSE',NULL,NULL),(127,'HKEX',NULL,NULL),(128,'HKFE',NULL,NULL),(129,'HKG',NULL,NULL),(130,'HKGI',NULL,NULL),(131,'HKME',NULL,NULL),(132,'HKSE',NULL,NULL),(133,'HMBSE',NULL,NULL),(134,'HSE',NULL,NULL),(135,'HSEI',NULL,NULL),(136,'HY',NULL,NULL),(137,'IBIS',NULL,NULL),(138,'ICE',NULL,NULL),(139,'ICEI',NULL,NULL),(140,'ICP',NULL,NULL),(141,'IDEAL','IDEAL IB FOREX','Eastern Standard Time'),(142,'IDEALPRO','IDEAL IB FOREX PRO','Eastern Standard Time'),(143,'IDEM',NULL,NULL),(144,'IDX',NULL,NULL),(145,'IGB',NULL,NULL),(146,'INDEX',NULL,NULL),(147,'INSNET',NULL,NULL),(148,'INT3B',NULL,NULL),(149,'INT3P',NULL,NULL),(150,'IO',NULL,NULL),(151,'IPE',NULL,NULL),(152,'IRISE',NULL,NULL),(153,'ISE',NULL,NULL),(154,'ISEI',NULL,NULL),(155,'ISLAND',NULL,NULL),(156,'JAKSE',NULL,NULL),(157,'JASDA',NULL,NULL),(158,'JOH',NULL,NULL),(159,'JOHSE',NULL,NULL),(160,'JSE',NULL,NULL),(161,'KARSE',NULL,NULL),(162,'KCBOT',NULL,NULL),(163,'KCBT',NULL,NULL),(164,'KLS',NULL,NULL),(165,'KLSI',NULL,NULL),(166,'KOQ',NULL,NULL),(167,'KOQI',NULL,NULL),(168,'KOR',NULL,NULL),(169,'KOREA',NULL,NULL),(170,'KORI',NULL,NULL),(171,'KRX',NULL,NULL),(172,'KSE',NULL,NULL),(173,'KUALA',NULL,NULL),(174,'KYOSE',NULL,NULL),(175,'LCE',NULL,NULL),(176,'LIFE',NULL,NULL),(177,'LIFFE',NULL,NULL),(178,'LIFFE_NF',NULL,NULL),(179,'LIMSE',NULL,NULL),(180,'LINC',NULL,NULL),(181,'LIS',NULL,NULL),(182,'LISSE',NULL,NULL),(183,'LME',NULL,NULL),(184,'LON',NULL,NULL),(185,'LSE',NULL,NULL),(186,'LSIN',NULL,NULL),(187,'LSO',NULL,NULL),(188,'LTO',NULL,NULL),(189,'LUX',NULL,NULL),(190,'LUXI',NULL,NULL),(191,'LUXSE',NULL,NULL),(192,'M',NULL,NULL),(193,'MAC',NULL,NULL),(194,'MADSE',NULL,NULL),(195,'MASE',NULL,NULL),(196,'MATF',NULL,NULL),(197,'MATIF',NULL,NULL),(198,'MATIFF',NULL,NULL),(199,'MBFX',NULL,NULL),(200,'MC',NULL,NULL),(201,'MCEDB',NULL,NULL),(202,'MDE',NULL,NULL),(203,'MDEI',NULL,NULL),(204,'ME',NULL,NULL),(205,'MEFF',NULL,NULL),(206,'MEFFO',NULL,NULL),(207,'MEFFRV',NULL,NULL),(208,'MEFFRY',NULL,NULL),(209,'MEX',NULL,NULL),(210,'MEXI',NULL,NULL),(211,'MEXSE',NULL,NULL),(212,'MF',NULL,NULL),(213,'MFE',NULL,NULL),(214,'MGE',NULL,NULL),(215,'MIC',NULL,NULL),(216,'MICEX',NULL,NULL),(217,'MIDAM',NULL,NULL),(218,'MIDWES',NULL,NULL),(219,'MM',NULL,NULL),(220,'MONE',NULL,NULL),(221,'MONEP',NULL,NULL),(222,'MSE',NULL,NULL),(223,'MSEI',NULL,NULL),(224,'MSO',NULL,NULL),(225,'MSPT',NULL,NULL),(226,'MT',NULL,NULL),(227,'MTS',NULL,NULL),(228,'MUN',NULL,NULL),(229,'MUNSE',NULL,NULL),(230,'MVSE',NULL,NULL),(231,'MX',NULL,NULL),(232,'N',NULL,NULL),(233,'NASD',NULL,NULL),(234,'NASDAQ','NASDAQ National Market','Eastern Standard Time'),(235,'NASDC',NULL,NULL),(236,'NASDSC',NULL,NULL),(237,'NAT',NULL,NULL),(238,'NB',NULL,NULL),(239,'NGYSE',NULL,NULL),(240,'NIGSE',NULL,NULL),(241,'NIKKEI',NULL,NULL),(242,'NLK',NULL,NULL),(243,'NLX',NULL,NULL),(244,'NO',NULL,NULL),(245,'NOTCBF',NULL,NULL),(246,'NPE',NULL,NULL),(247,'NQLX',NULL,NULL),(248,'NSE',NULL,NULL),(249,'NYBOT',NULL,NULL),(250,'NYCE',NULL,NULL),(251,'NYFE',NULL,NULL),(252,'NYLCD',NULL,NULL),(253,'NYLID',NULL,NULL),(254,'NYLUS',NULL,NULL),(255,'NYME',NULL,NULL),(256,'NYMEX',NULL,NULL),(257,'NYMI',NULL,NULL),(258,'NYSE','New York Stock Exchange','Eastern Standard Time'),(259,'NYSELIFFE',NULL,NULL),(260,'NZSE',NULL,NULL),(261,'NZX',NULL,NULL),(262,'NZXI',NULL,NULL),(263,'OETOB',NULL,NULL),(264,'OFX',NULL,NULL),(265,'OM',NULL,NULL),(266,'OMFE',NULL,NULL),(267,'OMS',NULL,NULL),(268,'ONE',NULL,NULL),(269,'OPRA',NULL,NULL),(270,'OPTS',NULL,NULL),(271,'OSASE',NULL,NULL),(272,'OSE',NULL,NULL),(273,'OSE.JPN',NULL,NULL),(274,'OSEI',NULL,NULL),(275,'OSL',NULL,NULL),(276,'OSLI',NULL,NULL),(277,'OSLSE',NULL,NULL),(278,'OT',NULL,NULL),(279,'OTC',NULL,NULL),(280,'OTCBB',NULL,NULL),(281,'P',NULL,NULL),(282,'PACIFI',NULL,NULL),(283,'PAR',NULL,NULL),(284,'PASE',NULL,NULL),(285,'PBT',NULL,NULL),(286,'PF',NULL,NULL),(287,'PHLX',NULL,NULL),(288,'PHPSE',NULL,NULL),(289,'PO',NULL,NULL),(290,'PRIMI',NULL,NULL),(291,'PSE',NULL,NULL),(292,'PSOFT',NULL,NULL),(293,'Q',NULL,NULL),(294,'RIOSE',NULL,NULL),(295,'RTS',NULL,NULL),(296,'S',NULL,NULL),(297,'SANSE',NULL,NULL),(298,'SAPSE',NULL,NULL),(299,'SBF',NULL,NULL),(300,'SEAQ',NULL,NULL),(301,'SEAQL2',NULL,NULL),(302,'SEAQTR',NULL,NULL),(303,'SES',NULL,NULL),(304,'SESI',NULL,NULL),(305,'SET',NULL,NULL),(306,'SETI',NULL,NULL),(307,'SFB',NULL,NULL),(308,'SFE',NULL,NULL),(309,'SGX',NULL,NULL),(310,'SHANG',NULL,NULL),(311,'SHENZ',NULL,NULL),(312,'SICOVA',NULL,NULL),(313,'SIMEX',NULL,NULL),(314,'SIMX',NULL,NULL),(315,'SING',NULL,NULL),(316,'SMART','Smart','Eastern Standard Time'),(317,'SNFE',NULL,NULL),(318,'SOFET',NULL,NULL),(319,'SOFFEX',NULL,NULL),(320,'SPAIN',NULL,NULL),(321,'SPCC',NULL,NULL),(322,'SPE',NULL,NULL),(323,'SPECI',NULL,NULL),(324,'SPSE',NULL,NULL),(325,'SSE',NULL,NULL),(326,'SSEI',NULL,NULL),(327,'STKSE',NULL,NULL),(328,'STREET',NULL,NULL),(329,'STX',NULL,NULL),(330,'SWB',NULL,NULL),(331,'SWE',NULL,NULL),(332,'SWEI',NULL,NULL),(333,'SWX',NULL,NULL),(334,'SWXI',NULL,NULL),(335,'T',NULL,NULL),(336,'TAISE',NULL,NULL),(337,'TC',NULL,NULL),(338,'TELSE',NULL,NULL),(339,'TFE',NULL,NULL),(340,'TIFFE',NULL,NULL),(341,'TSE','Toronto Stock Exchange','Eastern Standard Time'),(342,'TSE.JPN',NULL,NULL),(343,'TSEI',NULL,NULL),(344,'TSO',NULL,NULL),(345,'TUL',NULL,NULL),(346,'U',NULL,NULL),(347,'UNDEF',NULL,NULL),(348,'USDA',NULL,NULL),(349,'USDC',NULL,NULL),(350,'UT',NULL,NULL),(351,'V',NULL,NULL),(352,'VC',NULL,NULL),(353,'VIE',NULL,NULL),(354,'VIEI',NULL,NULL),(355,'VIESE',NULL,NULL),(356,'VIRTX',NULL,NULL),(357,'VSE',NULL,NULL),(358,'VSO',NULL,NULL),(359,'VWAP',NULL,NULL),(360,'W',NULL,NULL),(361,'WAR',NULL,NULL),(362,'WARI',NULL,NULL),(363,'WBE',NULL,NULL),(364,'WBEI',NULL,NULL),(365,'WCE',NULL,NULL),(366,'WOLFF',NULL,NULL),(367,'X',NULL,NULL),(368,'XET',NULL,NULL),(369,'XETRA',NULL,NULL),(370,'XO',NULL,NULL),(371,'ZS',NULL,NULL),(372,'ZURIB',NULL,NULL),(373,'VENTURE',NULL,NULL),(374,'CHX',NULL,NULL),(375,'DRCTEDGE',NULL,NULL),(376,'NSX',NULL,NULL),(377,'BEX',NULL,NULL),(378,'CBSX',NULL,NULL),(379,'BATS',NULL,NULL),(380,'EDGEA',NULL,NULL),(381,'CHIXEN',NULL,NULL),(382,'BATEEN',NULL,NULL),(383,'VALUE',NULL,NULL),(384,'LAVA',NULL,NULL),(385,'CSFBALGO',NULL,NULL),(386,'JEFFALGO',NULL,NULL),(387,'BYX',NULL,NULL),(388,'PSX',NULL,NULL),(389,'GEMINI',NULL,NULL),(390,'NASDAQBX',NULL,NULL),(391,'NASDAQOM',NULL,NULL),(392,'CBOE2',NULL,NULL),(393,'MIAX',NULL,NULL),(394,'A','',NULL);
/*!40000 ALTER TABLE `exchanges` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `exchangesessions`
--

DROP TABLE IF EXISTS `exchangesessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `exchangesessions` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `ExchangeID` int(11) DEFAULT NULL,
  `OpeningTime` time NOT NULL,
  `ClosingTime` time NOT NULL,
  `IsSessionEnd` tinyint(1) NOT NULL,
  `OpeningDay` tinyint(4) NOT NULL,
  `ClosingDay` tinyint(4) NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `FK_exchangeSessions_exchanges_ID` (`ExchangeID`),
  CONSTRAINT `FK_exchangeSessions_exchanges_ID` FOREIGN KEY (`ExchangeID`) REFERENCES `exchanges` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=69 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `exchangesessions`
--

LOCK TABLES `exchangesessions` WRITE;
/*!40000 ALTER TABLE `exchangesessions` DISABLE KEYS */;
INSERT INTO `exchangesessions` VALUES (1,316,'08:00:00','18:30:00',1,0,0),(14,316,'08:00:00','18:30:00',1,1,1),(16,316,'08:00:00','18:30:00',1,2,2),(17,59,'00:00:00','23:59:00',1,0,0),(18,59,'00:00:00','23:59:00',1,1,1),(19,59,'00:00:00','23:59:00',1,2,2),(20,59,'00:00:00','23:59:00',1,3,3),(21,59,'00:00:00','23:59:00',1,4,4),(22,66,'09:30:00','16:15:00',1,0,0),(23,66,'09:30:00','16:15:00',1,1,1),(24,66,'09:30:00','16:15:00',1,2,2),(25,66,'09:30:00','16:15:00',1,3,3),(26,66,'09:30:00','16:15:00',1,4,4),(27,316,'08:00:00','18:30:00',1,3,3),(28,316,'08:00:00','18:30:00',1,4,4),(29,70,'00:00:00','23:59:00',1,0,0),(30,70,'00:00:00','23:59:00',1,1,1),(31,70,'00:00:00','23:59:00',1,2,2),(32,70,'00:00:00','23:59:00',1,3,3),(33,70,'00:00:00','23:59:00',1,4,4),(34,234,'09:30:00','16:00:00',1,0,0),(35,234,'09:30:00','16:00:00',1,1,1),(36,234,'09:30:00','16:00:00',1,2,2),(37,234,'09:30:00','16:00:00',1,3,3),(38,234,'09:30:00','16:00:00',1,4,4),(39,258,'09:30:00','16:00:00',1,0,0),(40,258,'09:30:00','16:00:00',1,1,1),(41,258,'09:30:00','16:00:00',1,2,2),(42,258,'09:30:00','16:00:00',1,3,3),(43,258,'09:30:00','16:00:00',1,4,4),(44,120,'00:00:00','23:59:00',1,0,0),(45,120,'00:00:00','23:59:00',1,1,1),(46,120,'00:00:00','23:59:00',1,2,2),(47,120,'00:00:00','23:59:00',1,3,3),(48,120,'00:00:00','23:59:00',1,4,4),(49,141,'00:00:00','23:59:00',1,0,0),(50,141,'00:00:00','23:59:00',1,1,1),(51,141,'00:00:00','23:59:00',1,2,2),(52,141,'00:00:00','23:59:00',1,3,3),(53,141,'00:00:00','23:59:00',1,4,4),(54,142,'00:00:00','23:59:00',1,0,0),(55,142,'00:00:00','23:59:00',1,1,1),(56,142,'00:00:00','23:59:00',1,2,2),(57,142,'00:00:00','23:59:00',1,3,3),(58,142,'00:00:00','23:59:00',1,4,4),(59,60,'00:00:00','23:59:00',1,0,0),(60,60,'00:00:00','23:59:00',1,1,1),(61,60,'00:00:00','23:59:00',1,2,2),(62,60,'00:00:00','23:59:00',1,3,3),(63,60,'00:00:00','23:59:00',1,4,4),(64,341,'09:30:00','16:00:00',1,0,0),(65,341,'09:30:00','16:00:00',1,1,1),(66,341,'09:30:00','16:00:00',1,2,2),(67,341,'09:30:00','16:00:00',1,3,3),(68,341,'09:30:00','16:00:00',1,4,4);
/*!40000 ALTER TABLE `exchangesessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `instruments`
--

DROP TABLE IF EXISTS `instruments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `instruments` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Symbol` varchar(50) DEFAULT NULL,
  `Name` varchar(150) DEFAULT NULL,
  `UnderlyingSymbol` varchar(50) DEFAULT NULL,
  `Type` int(5) NOT NULL,
  `Expiration` date DEFAULT NULL,
  `Strike` decimal(16,8) DEFAULT NULL,
  `OptionType` tinyint(4) DEFAULT NULL,
  `Multiplier` int(11) NOT NULL DEFAULT '1',
  `PrimaryExchangeID` int(11) DEFAULT NULL,
  `ExchangeID` int(11) DEFAULT NULL,
  `ValidExchanges` varchar(255) DEFAULT NULL,
  `DatasourceID` int(11) DEFAULT NULL,
  `IsContinuousFuture` tinyint(1) NOT NULL DEFAULT '0',
  `ContinuousFutureID` int(11) DEFAULT NULL,
  `MinTick` decimal(12,7) DEFAULT NULL,
  `Currency` varchar(50) DEFAULT NULL,
  `Industry` varchar(255) DEFAULT NULL,
  `Category` varchar(255) DEFAULT NULL,
  `Subcategory` varchar(255) DEFAULT NULL,
  `SessionsSource` tinyint(4) NOT NULL DEFAULT '0',
  `SessionTemplateID` int(11) DEFAULT NULL,
  `DatasourceSymbol` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UK_instruments_continuous_future_id` (`ContinuousFutureID`),
  UNIQUE KEY `UK_instruments` (`DatasourceID`,`ExchangeID`,`Symbol`,`Expiration`),
  KEY `FK_instruments_exchanges_ID` (`ExchangeID`),
  KEY `FK_instruments_exchanges_id2` (`PrimaryExchangeID`),
  CONSTRAINT `FK_instruments_continuousfutures_id` FOREIGN KEY (`ContinuousFutureID`) REFERENCES `continuousfutures` (`ID`),
  CONSTRAINT `FK_instruments_datasources_id` FOREIGN KEY (`DatasourceID`) REFERENCES `datasources` (`ID`) ON UPDATE NO ACTION,
  CONSTRAINT `FK_instruments_exchanges_ID` FOREIGN KEY (`ExchangeID`) REFERENCES `exchanges` (`ID`) ON UPDATE NO ACTION,
  CONSTRAINT `FK_instruments_exchanges_id2` FOREIGN KEY (`PrimaryExchangeID`) REFERENCES `exchanges` (`ID`) ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=386 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `instruments`
--

LOCK TABLES `instruments` WRITE;
/*!40000 ALTER TABLE `instruments` DISABLE KEYS */;
INSERT INTO `instruments` VALUES (1,'SPY','SPDR S&P 500 ETF TRUST','SPY',0,NULL,0.00000000,NULL,1,10,316,'SMART,ISE,CHX,ARCA,ISLAND,DRCTEDGE,NSX,BEX,CBSX,BATS,EDGEA,LAVA,CSFBALGO,JEFFALGO,BYX,PSX',2,0,NULL,0.0100000,'USD','Funds','Equity Fund','Growth&Income-Large Cap',1,1,NULL);
/*!40000 ALTER TABLE `instruments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `instrumentsessions`
--

DROP TABLE IF EXISTS `instrumentsessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `instrumentsessions` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `InstrumentID` int(11) DEFAULT NULL,
  `OpeningTime` time NOT NULL,
  `ClosingTime` time NOT NULL,
  `IsSessionEnd` tinyint(1) NOT NULL,
  `OpeningDay` tinyint(4) NOT NULL,
  `ClosingDay` tinyint(4) NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `IDX_instrumentsessions_InstrumentID` (`InstrumentID`),
  CONSTRAINT `FK_instrumentsessions_instruments_ID` FOREIGN KEY (`InstrumentID`) REFERENCES `instruments` (`ID`) ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1978 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `instrumentsessions`
--

LOCK TABLES `instrumentsessions` WRITE;
/*!40000 ALTER TABLE `instrumentsessions` DISABLE KEYS */;
INSERT INTO `instrumentsessions` (`ID`, `InstrumentID`, `OpeningTime`, `ClosingTime`, `IsSessionEnd`, `OpeningDay`, `ClosingDay`) VALUES
(1, 1, '09:30:00', '16:00:00', 1, 3, 3),
(2, 1, '09:30:00', '16:00:00', 1, 0, 0),
(3, 1, '09:30:00', '16:00:00', 1, 4, 4),
(4, 1, '09:30:00', '16:00:00', 1, 1, 1),
(5, 1, '09:30:00', '16:00:00', 1, 2, 2);
/*!40000 ALTER TABLE `instrumentsessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sessiontemplates`
--

DROP TABLE IF EXISTS `sessiontemplates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sessiontemplates` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sessiontemplates`
--

LOCK TABLES `sessiontemplates` WRITE;
/*!40000 ALTER TABLE `sessiontemplates` DISABLE KEYS */;
INSERT INTO `sessiontemplates` VALUES (1,'U.S. Equities RTH'),(2,'U.S. Equities (w/ Post)'),(5,'U.S. Equities (w/ Pre)'),(6,'U.S. Equities (w/ Pre & Post)'),(7,'CME: Equity Index Futures (GLOBEX)'),(8,'CME: Equity Index Futures (Open Outcry)'),(9,'CME: Equity Index Futures [E-Mini] (GLOBEX)'),(10,'CME: FX Futures (GLOBEX)');
/*!40000 ALTER TABLE `sessiontemplates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tag_map`
--

DROP TABLE IF EXISTS `tag_map`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tag_map` (
  `InstrumentID` int(11) NOT NULL,
  `TagID` int(11) NOT NULL,
  PRIMARY KEY (`InstrumentID`,`TagID`),
  KEY `FK_tag_map_tags_id` (`TagID`),
  CONSTRAINT `FK_tag_map_instruments_id` FOREIGN KEY (`InstrumentID`) REFERENCES `instruments` (`ID`),
  CONSTRAINT `FK_tag_map_tags_id` FOREIGN KEY (`TagID`) REFERENCES `tags` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tags`
--

DROP TABLE IF EXISTS `tags`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tags` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;


--
-- Table structure for table `templatesessions`
--

DROP TABLE IF EXISTS `templatesessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `templatesessions` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `TemplateID` int(11) DEFAULT NULL,
  `OpeningTime` time NOT NULL,
  `ClosingTime` time NOT NULL,
  `IsSessionEnd` tinyint(1) NOT NULL,
  `OpeningDay` tinyint(4) NOT NULL,
  `ClosingDay` tinyint(4) NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=55 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `templatesessions`
--

LOCK TABLES `templatesessions` WRITE;
/*!40000 ALTER TABLE `templatesessions` DISABLE KEYS */;
INSERT INTO `templatesessions` VALUES (4,1,'09:30:00','16:00:00',1,0,0),(5,1,'09:30:00','16:00:00',1,1,1),(6,1,'09:30:00','16:00:00',1,2,2),(7,1,'09:30:00','16:00:00',1,3,3),(8,1,'09:30:00','16:00:00',1,4,4),(9,2,'09:30:00','20:00:00',1,0,0),(10,2,'09:30:00','20:00:00',1,1,1),(11,2,'09:30:00','20:00:00',1,2,2),(12,2,'09:30:00','20:00:00',1,3,3),(13,2,'09:30:00','20:00:00',1,4,4),(17,5,'08:00:00','16:00:00',1,0,0),(18,5,'08:00:00','16:00:00',1,1,1),(19,5,'08:00:00','16:00:00',1,2,2),(20,5,'08:00:00','16:00:00',1,3,3),(21,5,'08:00:00','06:00:00',1,4,4),(22,6,'08:00:00','20:00:00',1,0,0),(23,6,'08:00:00','20:00:00',1,1,1),(24,6,'08:00:00','20:00:00',1,2,2),(25,6,'08:00:00','20:00:00',1,3,3),(26,6,'08:00:00','20:00:00',1,4,4),(27,7,'15:30:00','16:30:00',1,0,0),(28,7,'17:00:00','08:15:00',0,0,1),(29,7,'15:30:00','16:30:00',1,1,1),(30,7,'17:00:00','08:15:00',0,1,2),(31,7,'15:30:00','16:30:00',1,2,2),(32,7,'17:00:00','08:15:00',0,2,3),(33,7,'15:30:00','16:30:00',1,3,3),(34,7,'17:00:00','08:15:00',1,3,4),(35,7,'17:00:00','08:15:00',0,6,0),(36,8,'08:30:00','15:15:00',1,0,0),(37,8,'08:30:00','15:15:00',1,1,1),(38,8,'08:30:00','15:15:00',1,2,2),(39,8,'08:30:00','15:15:00',1,3,3),(40,8,'08:30:00','15:15:00',1,4,4),(41,9,'15:30:00','16:30:00',1,0,0),(42,9,'17:00:00','15:15:00',0,0,1),(43,9,'15:30:00','16:30:00',1,1,1),(44,9,'17:00:00','15:15:00',0,1,2),(45,9,'15:30:00','16:30:00',1,2,2),(46,9,'17:00:00','15:15:00',0,2,3),(47,9,'15:30:00','16:30:00',1,3,3),(48,9,'17:00:00','15:15:00',1,3,4),(49,9,'17:00:00','15:15:00',0,6,0),(50,10,'17:00:00','16:00:00',1,0,1),(51,10,'17:00:00','16:00:00',1,1,2),(52,10,'17:00:00','16:00:00',1,2,3),(53,10,'17:00:00','16:00:00',1,3,4),(54,10,'17:00:00','16:00:00',1,6,0);
/*!40000 ALTER TABLE `templatesessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `underlyingsymbols`
--

DROP TABLE IF EXISTS `underlyingsymbols`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `underlyingsymbols` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Symbol` varchar(255) DEFAULT NULL,
  `ExpirationRule` blob,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping events for database 'qdms'
--

--
-- Dumping routines for database 'qdms'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2013-12-06 18:54:05
