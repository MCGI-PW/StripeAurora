-- =====================================================================================
-- Aurora - DDL do banco de dados Oracle
-- =====================================================================================
-- Este script reproduz exatamente o schema gerado pela migration do Entity Framework
-- Core (Aurora/Infrastructure/Data/Migrations). Ele existe para leitura/entrega; o caminho
-- oficial de criacao do banco continua sendo `dotnet ef database update`.
--
-- Banco.....: Oracle (servidor academico da FIAP - oracle.fiap.com.br:1521/orcl)
-- Schema....: usuario RM do aluno
-- Migration.: 20260914014332_InitialCreate
--
-- Modelo:
--   TB_CLINIC 1 ----< N TB_CLINIC_SUBSCRIPTION N >---- 1 TB_SUBSCRIPTION_PLAN
--   (a clinica assina um plano; a assinatura e a associativa entre as duas pontas)
-- =====================================================================================

-- -------------------------------------------------------------------------------------
-- Limpeza (execute apenas se quiser recriar do zero; a ordem respeita as FKs)
-- -------------------------------------------------------------------------------------
-- DROP TABLE TB_CLINIC_SUBSCRIPTION CASCADE CONSTRAINTS;
-- DROP TABLE TB_SUBSCRIPTION_PLAN   CASCADE CONSTRAINTS;
-- DROP TABLE TB_CLINIC              CASCADE CONSTRAINTS;


-- =====================================================================================
-- TABELA 1 - TB_CLINIC
-- Clinica veterinaria: a assinante dos planos e o tenant de toda a plataforma.
-- =====================================================================================
CREATE TABLE TB_CLINIC (
    ID           RAW(16)         NOT NULL,
    NAME         NVARCHAR2(150)  NOT NULL,
    CNPJ         NVARCHAR2(14)   NOT NULL,
    EMAIL        NVARCHAR2(150)  NOT NULL,
    FIREBASE_UID NVARCHAR2(128)  NOT NULL,
    ACTIVE       NUMBER(1)       NOT NULL,
    CONSTRAINT PK_TB_CLINIC PRIMARY KEY (ID)
);

COMMENT ON TABLE  TB_CLINIC              IS 'Clinicas veterinarias cadastradas no Aurora. E a entidade que contrata os planos de assinatura.';
COMMENT ON COLUMN TB_CLINIC.ID           IS 'Chave primaria da clinica (GUID gerado pela aplicacao).';
COMMENT ON COLUMN TB_CLINIC.NAME         IS 'Razao social ou nome fantasia da clinica.';
COMMENT ON COLUMN TB_CLINIC.CNPJ         IS 'CNPJ da clinica, somente digitos.';
COMMENT ON COLUMN TB_CLINIC.EMAIL        IS 'E-mail administrativo usado na cobranca.';
COMMENT ON COLUMN TB_CLINIC.FIREBASE_UID IS 'UID da conta da clinica no Firebase Authentication.';
COMMENT ON COLUMN TB_CLINIC.ACTIVE       IS '1 = clinica ativa na plataforma, 0 = desativada.';

-- Uma clinica nunca se cadastra duas vezes com o mesmo CNPJ.
CREATE UNIQUE INDEX UX_CLINIC_CNPJ ON TB_CLINIC (CNPJ);

-- Cada conta do Firebase corresponde a exatamente uma clinica.
CREATE UNIQUE INDEX UX_CLINIC_FIREBASE_UID ON TB_CLINIC (FIREBASE_UID);


-- =====================================================================================
-- TABELA 2 - TB_SUBSCRIPTION_PLAN
-- Catalogo de planos comercializados, espelhando os Prices configurados na Stripe.
-- =====================================================================================
CREATE TABLE TB_SUBSCRIPTION_PLAN (
    ID              RAW(16)         NOT NULL,
    TIER            NVARCHAR2(20)   NOT NULL,
    NAME            NVARCHAR2(120)  NOT NULL,
    PRICE           NUMBER(18,2)    NOT NULL,
    STRIPE_PRICE_ID NVARCHAR2(120)  NOT NULL,
    ACTIVE          NUMBER(1)       NOT NULL,
    CONSTRAINT PK_TB_SUBSCRIPTION_PLAN PRIMARY KEY (ID)
);

COMMENT ON TABLE  TB_SUBSCRIPTION_PLAN                 IS 'Catalogo de planos de assinatura do Aurora, espelhando os precos cadastrados na Stripe.';
COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.ID              IS 'Chave primaria do plano (GUID gerado pela aplicacao).';
COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.TIER            IS 'Faixa do plano: Basic ou Premium.';
COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.NAME            IS 'Nome comercial do plano exibido na contratacao.';
COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.PRICE           IS 'Preco mensal do plano em reais.';
COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.STRIPE_PRICE_ID IS 'Identificador do Price correspondente na Stripe.';
COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.ACTIVE          IS '1 = plano disponivel para novas assinaturas, 0 = descontinuado.';

-- Um price da Stripe nunca pode estar vinculado a dois planos diferentes.
CREATE UNIQUE INDEX UX_PLAN_STRIPE_PRICE ON TB_SUBSCRIPTION_PLAN (STRIPE_PRICE_ID);


-- =====================================================================================
-- TABELA 3 - TB_CLINIC_SUBSCRIPTION
-- Associativa: liga a clinica ao plano contratado e espelha a subscription da Stripe.
-- =====================================================================================
CREATE TABLE TB_CLINIC_SUBSCRIPTION (
    ID                     RAW(16)         NOT NULL,
    CLINIC_ID              RAW(16)         NOT NULL,
    PLAN_ID                RAW(16)         NOT NULL,
    STRIPE_CUSTOMER_ID     NVARCHAR2(120)  NOT NULL,
    STRIPE_SUBSCRIPTION_ID NVARCHAR2(120)  NOT NULL,
    STATUS                 NVARCHAR2(20)   NOT NULL,
    CURRENT_PERIOD_END     TIMESTAMP(7)    NOT NULL,
    CONSTRAINT PK_TB_CLINIC_SUBSCRIPTION PRIMARY KEY (ID),
    CONSTRAINT FK_SUBSCRIPTION_CLINIC FOREIGN KEY (CLINIC_ID)
        REFERENCES TB_CLINIC (ID),
    CONSTRAINT FK_SUBSCRIPTION_PLAN FOREIGN KEY (PLAN_ID)
        REFERENCES TB_SUBSCRIPTION_PLAN (ID)
);

COMMENT ON TABLE  TB_CLINIC_SUBSCRIPTION                        IS 'Assinaturas contratadas pelas clinicas: associa TB_CLINIC a TB_SUBSCRIPTION_PLAN e espelha a subscription da Stripe.';
COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.ID                     IS 'Chave primaria da assinatura (GUID gerado pela aplicacao).';
COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.CLINIC_ID              IS 'Chave estrangeira para TB_CLINIC.ID - a clinica que contratou o plano.';
COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.PLAN_ID                IS 'Chave estrangeira para TB_SUBSCRIPTION_PLAN.ID - o plano contratado.';
COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.STRIPE_CUSTOMER_ID     IS 'Identificador do Customer correspondente na Stripe.';
COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.STRIPE_SUBSCRIPTION_ID IS 'Identificador da Subscription correspondente na Stripe.';
COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.STATUS                 IS 'Ciclo de vida da assinatura: Incomplete, Active, PastDue ou Canceled.';
COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.CURRENT_PERIOD_END     IS 'Data/hora em que o periodo pago corrente se encerra.';

-- Consulta mais frequente da API: quais assinaturas pertencem a esta clinica.
CREATE INDEX IX_SUBSCRIPTION_CLINIC ON TB_CLINIC_SUBSCRIPTION (CLINIC_ID);

-- Acelera o JOIN e a verificacao da FK ao excluir um plano.
CREATE INDEX IX_TB_CLINIC_SUBSCRIPTION_PLAN_ID ON TB_CLINIC_SUBSCRIPTION (PLAN_ID);

-- Uma subscription da Stripe corresponde a no maximo uma linha aqui (idempotencia de webhook).
CREATE UNIQUE INDEX UX_SUBSCRIPTION_STRIPE_ID ON TB_CLINIC_SUBSCRIPTION (STRIPE_SUBSCRIPTION_ID);


-- =====================================================================================
-- CONTROLE DE MIGRATIONS (criada e mantida pelo EF Core)
-- =====================================================================================
CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId"    NVARCHAR2(150) NOT NULL,
    "ProductVersion" NVARCHAR2(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

COMMENT ON TABLE "__EFMigrationsHistory" IS 'Historico de migrations aplicadas pelo Entity Framework Core.';


-- =====================================================================================
-- CARGA INICIAL - conteudo significativo nas tres tabelas
-- Corresponde ao HasData das configurations; aplicada pela migration InitialCreate.
-- =====================================================================================
INSERT INTO TB_CLINIC (ID, NAME, CNPJ, EMAIL, FIREBASE_UID, ACTIVE)
VALUES (HEXTORAW('AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'), 'Clinica Vida Animal', '12345678000190',
        'contato@vidaanimal.com.br', 'uid-clinica-vida-animal', 1);

INSERT INTO TB_CLINIC (ID, NAME, CNPJ, EMAIL, FIREBASE_UID, ACTIVE)
VALUES (HEXTORAW('BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB'), 'Clinica Patas Felizes', '98765432000155',
        'financeiro@patasfelizes.com.br', 'uid-clinica-patas-felizes', 1);

INSERT INTO TB_SUBSCRIPTION_PLAN (ID, TIER, NAME, PRICE, STRIPE_PRICE_ID, ACTIVE)
VALUES (HEXTORAW('11111111111111111111111111111111'), 'Basic', 'Aurora Basico', 29.90, 'price_aurora_basico', 1);

INSERT INTO TB_SUBSCRIPTION_PLAN (ID, TIER, NAME, PRICE, STRIPE_PRICE_ID, ACTIVE)
VALUES (HEXTORAW('22222222222222222222222222222222'), 'Premium', 'Aurora Premium', 59.90, 'price_aurora_premium', 1);

INSERT INTO TB_CLINIC_SUBSCRIPTION (ID, CLINIC_ID, PLAN_ID, STRIPE_CUSTOMER_ID, STRIPE_SUBSCRIPTION_ID, STATUS, CURRENT_PERIOD_END)
VALUES (HEXTORAW('33333333333333333333333333333333'),
        HEXTORAW('AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'),
        HEXTORAW('22222222222222222222222222222222'),
        'cus_aurora_vida_animal', 'sub_aurora_vida_animal', 'Active',
        TO_TIMESTAMP('2026-12-31 23:59:59', 'YYYY-MM-DD HH24:MI:SS'));

INSERT INTO TB_CLINIC_SUBSCRIPTION (ID, CLINIC_ID, PLAN_ID, STRIPE_CUSTOMER_ID, STRIPE_SUBSCRIPTION_ID, STATUS, CURRENT_PERIOD_END)
VALUES (HEXTORAW('44444444444444444444444444444444'),
        HEXTORAW('BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB'),
        HEXTORAW('11111111111111111111111111111111'),
        'cus_aurora_patas_felizes', 'sub_aurora_patas_felizes', 'Incomplete',
        TO_TIMESTAMP('2026-10-31 23:59:59', 'YYYY-MM-DD HH24:MI:SS'));

COMMIT;


-- =====================================================================================
-- CONSULTAS DE EVIDENCIA (usadas no video para comprovar cada operacao do CRUD)
-- =====================================================================================
-- Clinicas cadastradas:
--   SELECT NAME, CNPJ, EMAIL, ACTIVE FROM TB_CLINIC ORDER BY NAME;
--
-- Planos cadastrados:
--   SELECT NAME, TIER, PRICE, STRIPE_PRICE_ID, ACTIVE FROM TB_SUBSCRIPTION_PLAN ORDER BY PRICE;
--
-- Assinaturas com a clinica e o plano (evidencia do relacionamento entre as tres tabelas):
--   SELECT c.NAME AS CLINICA, c.CNPJ, p.NAME AS PLANO, p.PRICE, s.STATUS, s.CURRENT_PERIOD_END
--     FROM TB_CLINIC_SUBSCRIPTION s
--     JOIN TB_CLINIC              c ON c.ID = s.CLINIC_ID
--     JOIN TB_SUBSCRIPTION_PLAN   p ON p.ID = s.PLAN_ID
--    ORDER BY c.NAME;
--
-- Estrutura e comentarios:
--   SELECT table_name, comments FROM user_tab_comments
--    WHERE table_name IN ('TB_CLINIC','TB_SUBSCRIPTION_PLAN','TB_CLINIC_SUBSCRIPTION');
--   SELECT table_name, column_name, comments FROM user_col_comments
--    WHERE table_name IN ('TB_CLINIC','TB_SUBSCRIPTION_PLAN','TB_CLINIC_SUBSCRIPTION') ORDER BY 1, 2;
--
-- Migrations aplicadas:
--   SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
