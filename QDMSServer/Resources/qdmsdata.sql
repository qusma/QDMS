CREATE DATABASE  IF NOT EXISTS `qdmsdata` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `qdmsdata`;
-- MySQL dump 10.13  Distrib 5.6.13, for Win32 (x86)
--
-- Host: 127.0.0.1    Database: qdmsdata
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
-- Table structure for table `data`
--

DROP TABLE IF EXISTS `data`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `data` (
  `DT` datetime(3) NOT NULL,
  `InstrumentID` int(11) NOT NULL,
  `Frequency` int(11) NOT NULL,
  `Open` decimal(16,8) NOT NULL,
  `High` decimal(16,8) NOT NULL,
  `Low` decimal(16,8) NOT NULL,
  `Close` decimal(16,8) NOT NULL,
  `AdjOpen` decimal(16,8) DEFAULT NULL,
  `AdjHigh` decimal(16,8) DEFAULT NULL,
  `AdjLow` decimal(16,8) DEFAULT NULL,
  `AdjClose` decimal(16,8) DEFAULT NULL,
  `Volume` bigint(20) DEFAULT NULL,
  `OpenInterest` int(11) DEFAULT NULL,
  `Dividend` decimal(16,8) DEFAULT NULL,
  `Split` decimal(16,8) DEFAULT NULL,
  PRIMARY KEY (`InstrumentID`,`Frequency`,`DT`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `instrumentinfo`
--

DROP TABLE IF EXISTS `instrumentinfo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `instrumentinfo` (
  `InstrumentID` int(11) NOT NULL,
  `Frequency` int(11) NOT NULL,
  `SourceDataFrequency` int(11) NOT NULL,
  `EarliestAvailableDT` datetime(3) DEFAULT NULL,
  `LatestAvailableDT` datetime(3) DEFAULT NULL,
  PRIMARY KEY (`InstrumentID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping events for database 'qdmsdata'
--

--
-- Dumping routines for database 'qdmsdata'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2013-12-06 18:54:29
