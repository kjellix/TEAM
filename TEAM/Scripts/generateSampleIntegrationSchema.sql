IF OBJECT_ID('dbo.HUB_CUSTOMER', 'U') IS NOT NULL DROP TABLE dbo.HUB_CUSTOMER
IF OBJECT_ID('dbo.HUB_INCENTIVE_OFFER', 'U') IS NOT NULL DROP TABLE dbo.HUB_INCENTIVE_OFFER
IF OBJECT_ID('dbo.HUB_MEMBERSHIP_PLAN', 'U') IS NOT NULL DROP TABLE dbo.HUB_MEMBERSHIP_PLAN
IF OBJECT_ID('dbo.HUB_SEGMENT', 'U') IS NOT NULL DROP TABLE dbo.HUB_SEGMENT
IF OBJECT_ID('dbo.LNK_CUSTOMER_COSTING', 'U') IS NOT NULL DROP TABLE dbo.LNK_CUSTOMER_COSTING
IF OBJECT_ID('dbo.LNK_CUSTOMER_OFFER', 'U') IS NOT NULL DROP TABLE dbo.LNK_CUSTOMER_OFFER
IF OBJECT_ID('dbo.LNK_MEMBERSHIP', 'U') IS NOT NULL DROP TABLE dbo.LNK_MEMBERSHIP
IF OBJECT_ID('dbo.LNK_RENEWAL_MEMBERSHIP', 'U') IS NOT NULL DROP TABLE dbo.LNK_RENEWAL_MEMBERSHIP
IF OBJECT_ID('dbo.LSAT_CUSTOMER_COSTING', 'U') IS NOT NULL DROP TABLE dbo.LSAT_CUSTOMER_COSTING
IF OBJECT_ID('dbo.LSAT_CUSTOMER_OFFER', 'U') IS NOT NULL DROP TABLE dbo.LSAT_CUSTOMER_OFFER
IF OBJECT_ID('dbo.LSAT_MEMBERSHIP', 'U') IS NOT NULL DROP TABLE dbo.LSAT_MEMBERSHIP
IF OBJECT_ID('dbo.SAT_CUSTOMER', 'U') IS NOT NULL DROP TABLE dbo.SAT_CUSTOMER
IF OBJECT_ID('dbo.SAT_CUSTOMER_ADDITIONAL_DETAILS', 'U') IS NOT NULL DROP TABLE dbo.SAT_CUSTOMER_ADDITIONAL_DETAILS
IF OBJECT_ID('dbo.SAT_INCENTIVE_OFFER', 'U') IS NOT NULL DROP TABLE dbo.SAT_INCENTIVE_OFFER
IF OBJECT_ID('dbo.SAT_MEMBERSHIP_PLAN_DETAIL', 'U') IS NOT NULL DROP TABLE dbo.SAT_MEMBERSHIP_PLAN_DETAIL
IF OBJECT_ID('dbo.SAT_MEMBERSHIP_PLAN_VALUATION', 'U') IS NOT NULL DROP TABLE dbo.SAT_MEMBERSHIP_PLAN_VALUATION
IF OBJECT_ID('dbo.SAT_SEGMENT', 'U') IS NOT NULL DROP TABLE dbo.SAT_SEGMENT
IF OBJECT_ID('dbo.BR_MEMBERSHIP_OFFER', 'U') IS NOT NULL DROP TABLE dbo.BR_MEMBERSHIP_OFFER

-- HUB CUSTOMER
CREATE TABLE dbo.HUB_CUSTOMER
(
  CUSTOMER_HSH binary(16) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  CUSTOMER_ID nvarchar(100) NOT NULL,
  CONSTRAINT [PK_HUB_CUSTOMER] PRIMARY KEY NONCLUSTERED (CUSTOMER_HSH ASC)
)

CREATE UNIQUE CLUSTERED INDEX IX_HUB_CUSTOMER ON dbo.HUB_CUSTOMER
(
  CUSTOMER_ID ASC
)

-- HUB INCENTIVE OFFER
CREATE TABLE dbo.HUB_INCENTIVE_OFFER
(
  INCENTIVE_OFFER_HSH binary(16) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  OFFER_ID nvarchar(100) NOT NULL,
  CONSTRAINT [PK_HUB_INCENTIVE_OFFER] PRIMARY KEY NONCLUSTERED (INCENTIVE_OFFER_HSH ASC)
)

CREATE UNIQUE CLUSTERED INDEX IX_HUB_INCENTIVE_OFFER ON dbo.HUB_INCENTIVE_OFFER
(
    OFFER_ID ASC
)

-- HUB MEMBERSHIP PLAN
CREATE TABLE dbo.HUB_MEMBERSHIP_PLAN
(
  MEMBERSHIP_PLAN_HSH binary(16) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  PLAN_CODE nvarchar(100) NOT NULL,
  PLAN_SUFFIX nvarchar(100) NOT NULL,
  CONSTRAINT [PK_HUB_MEMBERSHIP_PLAN] PRIMARY KEY NONCLUSTERED (MEMBERSHIP_PLAN_HSH ASC)
)

CREATE UNIQUE CLUSTERED INDEX IX_HUB_MEMBERSHIP_PLAN ON dbo.HUB_MEMBERSHIP_PLAN
(
  PLAN_CODE ASC,
  PLAN_SUFFIX ASC
)

-- HUB SEGMENT
CREATE TABLE dbo.HUB_SEGMENT
(
  SEGMENT_HSH binary(16) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  SEGMENT_CODE nvarchar(100) NOT NULL,
  CONSTRAINT [PK_HUB_SEGMENT] PRIMARY KEY NONCLUSTERED (SEGMENT_HSH ASC)
)

CREATE UNIQUE CLUSTERED INDEX IX_HUB_SEGMENT ON dbo.HUB_SEGMENT
(
  SEGMENT_CODE ASC
)

-- LNK CUSTOMER COSTING
CREATE TABLE dbo.LNK_CUSTOMER_COSTING
(
  CUSTOMER_COSTING_HSH binary(16) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  MEMBERSHIP_PLAN_HSH binary(16) NOT NULL,
  CUSTOMER_HSH binary(16) NOT NULL,
  SEGMENT_HSH binary(16) NOT NULL,
  CONSTRAINT [PK_LNK_CUSTOMER_COSTING] PRIMARY KEY NONCLUSTERED (CUSTOMER_COSTING_HSH ASC)
)

CREATE UNIQUE CLUSTERED INDEX IX_LNK_CUSTOMER_COSTING ON dbo.LNK_CUSTOMER_COSTING
(
  CUSTOMER_HSH ASC,
  MEMBERSHIP_PLAN_HSH ASC,
  SEGMENT_HSH ASC
)

-- LNK CUSTOMER OFFER
CREATE TABLE dbo.LNK_CUSTOMER_OFFER
(
  CUSTOMER_OFFER_HSH binary(16) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  CUSTOMER_HSH binary(16) NOT NULL,
  INCENTIVE_OFFER_HSH binary(16) NOT NULL,
  CONSTRAINT [PK_LNK_CUSTOMER_OFFER] PRIMARY KEY NONCLUSTERED(CUSTOMER_OFFER_HSH ASC)
)

CREATE UNIQUE CLUSTERED INDEX IX_LNK_CUSTOMER_OFFER ON dbo.LNK_CUSTOMER_OFFER
(
  CUSTOMER_HSH ASC,
  INCENTIVE_OFFER_HSH ASC
)

EXEC sp_addextendedproperty
@name = 'Driving_Key_Indicator', @value = 'True',
@level0type = 'SCHEMA', @level0name = 'dbo',
@level1type = 'TABLE', @level1name = 'LNK_CUSTOMER_OFFER',
@level2type = 'COLUMN', @level2name = 'CUSTOMER_HSH'

-- LNK MEMBERSHIP
CREATE TABLE dbo.LNK_MEMBERSHIP
(
  MEMBERSHIP_HSH binary(16) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  CUSTOMER_HSH binary(16) NOT NULL,
  MEMBERSHIP_PLAN_HSH binary(16) NOT NULL,
  SALES_CHANNEL nvarchar(100) NOT NULL,
  CONSTRAINT [PK_LNK_MEMBERSHIP] PRIMARY KEY NONCLUSTERED(MEMBERSHIP_HSH ASC)
)

CREATE UNIQUE CLUSTERED INDEX IX_LNK_MEMBERSHIP ON dbo.LNK_MEMBERSHIP
(
  CUSTOMER_HSH ASC,
  MEMBERSHIP_PLAN_HSH ASC,
  SALES_CHANNEL ASC
)

-- LNK RENEWAL_MEMBERSHIP
CREATE TABLE[dbo].[LNK_RENEWAL_MEMBERSHIP]
(
  [RENEWAL_MEMBERSHIP_HSH][binary](16) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  [MEMBERSHIP_PLAN_HSH] [binary] (16) NOT NULL,
  [RENEWAL_PLAN_HSH] [binary] (16) NOT NULL,
  CONSTRAINT [PK_LNK_RENEWAL_MEMBERSHIP] PRIMARY KEY NONCLUSTERED ([RENEWAL_MEMBERSHIP_HSH] ASC)
) ON [PRIMARY]

CREATE UNIQUE CLUSTERED INDEX IX_LNK_RENEWAL_MEMBERSHIP ON dbo.LNK_RENEWAL_MEMBERSHIP
(
  [MEMBERSHIP_PLAN_HSH] ASC,
  [RENEWAL_PLAN_HSH] ASC
)

-- LSAT CUSTOMER COSTING
CREATE TABLE dbo.LSAT_CUSTOMER_COSTING
(
  CUSTOMER_COSTING_HSH binary(16) NOT NULL,
  COSTING_EFFECTIVE_DATE datetime2(7) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  PERSONAL_MONTHLY_COST numeric(38,20) NULL,
  CONSTRAINT [PK_LSAT_CUSTOMER_COSTING] PRIMARY KEY CLUSTERED (CUSTOMER_COSTING_HSH ASC, LOAD_DATETIME ASC
, COSTING_EFFECTIVE_DATE ASC)
)

-- LSAT CUSTOMER OFFER
CREATE TABLE dbo.LSAT_CUSTOMER_OFFER
(
  CUSTOMER_OFFER_HSH binary(16) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  CONSTRAINT [PK_LSAT_CUSTOMER_OFFER] PRIMARY KEY CLUSTERED (CUSTOMER_OFFER_HSH ASC, LOAD_DATETIME ASC
)
)

-- LSAT MEMBERSHIP
CREATE TABLE dbo.LSAT_MEMBERSHIP
(
  MEMBERSHIP_HSH binary(16) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  MEMBERSHIP_START_DATE datetime2(7) NULL,
  MEMBERSHIP_END_DATE datetime2(7) NULL,
  MEMBERSHIP_STATUS nvarchar(100) NULL,
  CONSTRAINT [PK_LSAT_MEMBERSHIP] PRIMARY KEY CLUSTERED (MEMBERSHIP_HSH ASC, LOAD_DATETIME ASC
)
)

-- SAT CUSTOMER
CREATE TABLE dbo.SAT_CUSTOMER
(
  CUSTOMER_HSH binary(16) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  GIVEN_NAME nvarchar(100) NULL,
  SURNAME nvarchar(100) NULL,
  SUBURB nvarchar(100) NULL,
  POSTCODE nvarchar(100) NULL,
  COUNTRY nvarchar(100) NULL,
  GENDER nvarchar(100) NULL,
  DATE_OF_BIRTH datetime2(7) NULL,
  REFERRAL_OFFER_MADE_INDICATOR nvarchar(100) NULL,
  CONSTRAINT [PK_SAT_CUSTOMER] PRIMARY KEY CLUSTERED (CUSTOMER_HSH ASC, LOAD_DATETIME ASC
)
)

-- SAT CUSTOMER ADDITIONAL DETAILS
CREATE TABLE dbo.SAT_CUSTOMER_ADDITIONAL_DETAILS
(
  CUSTOMER_HSH binary(16) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  CONTACT_NUMBER nvarchar(100) NULL,
  [STATE] nvarchar(100) NULL,
  CONSTRAINT [PK_SAT_CUSTOMER_ADDITIONAL_DETAILS] PRIMARY KEY CLUSTERED (CUSTOMER_HSH ASC, LOAD_DATETIME ASC
)
)

-- SAT INCENTIVE OFFER
CREATE TABLE dbo.SAT_INCENTIVE_OFFER
(
  INCENTIVE_OFFER_HSH binary(16) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  OFFER_DESCRIPTION nvarchar(100) NULL,
  CONSTRAINT [PK_SAT_INCENTIVE_OFFER] PRIMARY KEY CLUSTERED(INCENTIVE_OFFER_HSH ASC, LOAD_DATETIME ASC
)
)

-- SAT MEMBERSHIP PLAN DETAIL
CREATE TABLE dbo.SAT_MEMBERSHIP_PLAN_DETAIL
(
  MEMBERSHIP_PLAN_HSH binary(16) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  PLAN_DESCRIPTION nvarchar(100) NULL,
  CONSTRAINT [PK_SAT_MEMBERSHIP_PLAN_DETAIL] PRIMARY KEY CLUSTERED(MEMBERSHIP_PLAN_HSH ASC, LOAD_DATETIME ASC
)
)

-- SAT MEMBERSHIP PLAN VALUATION
CREATE TABLE dbo.SAT_MEMBERSHIP_PLAN_VALUATION
(
  MEMBERSHIP_PLAN_HSH binary(16) NOT NULL,
  PLAN_VALUATION_DATE datetime2(7) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  PLAN_VALUATION_AMOUNT numeric(38,20) NULL,
  CONSTRAINT [PK_SAT_MEMBERSHIP_PLAN_VALUATION] PRIMARY KEY CLUSTERED(MEMBERSHIP_PLAN_HSH ASC, LOAD_DATETIME ASC
, PLAN_VALUATION_DATE ASC)
)

-- SAT SEGMENT
CREATE TABLE dbo.SAT_SEGMENT
(
  SEGMENT_HSH binary(16) NOT NULL,
  LOAD_DATETIME datetime2(7) NOT NULL,
  LOAD_END_DATETIME datetime2(7) NOT NULL,
  CURRENT_RECORD_INDICATOR varchar(100) NOT NULL,
  ETL_INSERT_RUN_ID integer NOT NULL,
  ETL_UPDATE_RUN_ID integer NOT NULL,
  CDC_OPERATION varchar(100) NOT NULL,
  SOURCE_ROW_ID integer NOT NULL,
  RECORD_SOURCE varchar(100) NOT NULL,
  HASH_FULL_RECORD binary(16) NOT NULL,
  SEGMENT_DESCRIPTION nvarchar(100) NULL,
  CONSTRAINT [PK_SAT_SEGMENT] PRIMARY KEY CLUSTERED (SEGMENT_HSH ASC, LOAD_DATETIME ASC
)
)

-- BR MEMBERSHIP OFFER
CREATE TABLE[dbo].[BR_MEMBERSHIP_OFFER]
(
  [SNAPSHOT_DATETIME][datetime2](7) NOT NULL,
  [CUSTOMER_OFFER_HSH] binary(16) NOT NULL,
  [MEMBERSHIP_HSH] binary(16) NOT NULL,
  [CUSTOMER_HSH] binary(16) NOT NULL,
  [MEMBERSHIP_PLAN_HSH] binary(16) NOT NULL,
  [SALES_CHANNEL] [nvarchar] (100) NOT NULL,
  CONSTRAINT [PK_BR_MEMBERSHIP_OFFER] PRIMARY KEY CLUSTERED([SNAPSHOT_DATETIME] ASC, [CUSTOMER_OFFER_HSH] ASC, [MEMBERSHIP_HSH] ASC, [CUSTOMER_HSH] ASC, [MEMBERSHIP_PLAN_HSH] ASC, [SALES_CHANNEL] ASC)
) ON [PRIMARY]