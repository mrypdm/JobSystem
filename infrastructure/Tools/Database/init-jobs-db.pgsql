--
-- Creating users
--
CREATE ROLE "svc_jobs_webapi@postgres" WITH
    LOGIN
    NOSUPERUSER
    NOINHERIT
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION
    NOBYPASSRLS;
CREATE ROLE "svc_jobs_worker@postgres" WITH
    LOGIN
    NOSUPERUSER
    NOINHERIT
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION
    NOBYPASSRLS;

--
-- Creating database
--
CREATE DATABASE "Jobs"
    WITH
    OWNER = pg_database_owner
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    LOCALE_PROVIDER = 'libc'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;

GRANT ALL ON DATABASE "Jobs" TO pg_database_owner;
GRANT CONNECT ON DATABASE "Jobs" TO "svc_jobs_webapi@postgres";
GRANT CONNECT ON DATABASE "Jobs" TO "svc_jobs_worker@postgres";

--
-- Creating schema
--
CREATE SCHEMA IF NOT EXISTS pgdbo AUTHORIZATION pg_database_owner;
GRANT ALL ON SCHEMA pgdbo TO pg_database_owner;
GRANT USAGE ON SCHEMA pgdbo TO "svc_jobs_webapi@postgres";
GRANT USAGE ON SCHEMA pgdbo TO "svc_jobs_worker@postgres";

--
-- Creating table
--
CREATE TABLE IF NOT EXISTS pgdbo."Jobs"
(
    "Id" uuid NOT NULL,
    "Status" integer NOT NULL DEFAULT 0,
    "Timeout" interval NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "StartedAt" timestamp with time zone,
    "FinishedAt" timestamp with time zone,
    "Script" text NOT NULL,
    "Results" bytea,
    CONSTRAINT "PK_JOBS_ID" PRIMARY KEY ("Id"),
    CONSTRAINT "CT_RESULTS_LESS_THAN_500MB" CHECK (octet_length("Results") <= (50 * 1024 * 1024))
)
TABLESPACE pg_default;
ALTER TABLE IF EXISTS pgdbo."Jobs" OWNER to pg_database_owner;

--
-- Creating functions for svc_jobs_webapi
--

-- Adding new Jobs
CREATE OR REPLACE PROCEDURE pgdbo.p_jobs_add_new(IN job_id uuid, IN timeout interval, IN script text)
LANGUAGE 'plpgsql'
SECURITY DEFINER
AS $BODY$
DECLARE
    current_job boolean;
BEGIN
    SELECT EXISTS(SELECT 1 FROM pgdbo."Jobs" WHERE "Id" = job_id) into current_job;

    IF current_job THEN
        RAISE EXCEPTION 'Job has been already added';
    END IF;

    INSERT INTO pgdbo."Jobs" ("Id", "Timeout", "Script", "CreatedAt")
    VALUES (job_id, timeout, script, NOW());
END
$BODY$;

ALTER PROCEDURE pgdbo.p_jobs_add_new(uuid, interval, text) OWNER TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_jobs_add_new(uuid, interval, text) TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_jobs_add_new(uuid, interval, text) TO "svc_jobs_webapi@postgres";
REVOKE ALL ON PROCEDURE pgdbo.p_jobs_add_new(uuid, interval, text) FROM PUBLIC;

-- Getting Jobs results
CREATE OR REPLACE FUNCTION pgdbo.f_jobs_get_results(job_id uuid)
RETURNS TABLE("Status" integer, "StartedAt" timestamp without time zone, "FinishedAt" timestamp without time zone, "Results" bytea)
LANGUAGE 'sql'
SECURITY DEFINER
AS $BODY$
    SELECT "Status", "StartedAt", "FinishedAt", "Results"
    FROM pgdbo."Jobs"
    WHERE "Id" = job_id
$BODY$;

ALTER FUNCTION pgdbo.f_jobs_get_results(uuid) OWNER TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_jobs_get_results(uuid) TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_jobs_get_results(uuid) TO "svc_jobs_webapi@postgres";
REVOKE ALL ON FUNCTION pgdbo.f_jobs_get_results(uuid) FROM PUBLIC;

-- Marking Jobs as Lost
CREATE OR REPLACE FUNCTION pgdbo.f_jobs_set_lost(timeout interval)
RETURNS TABLE("Id" uuid)
LANGUAGE 'sql'
SECURITY DEFINER
AS $BODY$
    UPDATE pgdbo."Jobs"
    SET "Status" = 5, "FinishedAt" = NOW()
    WHERE "Status" <> 5 AND "CreatedAt" + timeout < NOW()
    RETURNING "Id"
$BODY$;

ALTER FUNCTION pgdbo.f_jobs_get_results(uuid) OWNER TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_jobs_get_results(uuid) TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_jobs_get_results(uuid) TO "svc_jobs_webapi@postgres";
REVOKE ALL ON FUNCTION pgdbo.f_jobs_get_results(uuid) FROM PUBLIC;

--
-- Creaging functions for svc_jobs_worker
--

-- Getting new Jobs
CREATE OR REPLACE FUNCTION pgdbo.f_jobs_get_new(job_id uuid)
RETURNS TABLE("Id" uuid, "Timeout" interval, "Script" text)
LANGUAGE 'sql'
SECURITY DEFINER
AS $BODY$
    SELECT "Id", "Timeout", "Script"
    FROM pgdbo."Jobs"
    WHERE "Id" = job_id
$BODY$;

ALTER FUNCTION pgdbo.f_jobs_get_new(uuid) OWNER TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_jobs_get_new(uuid) TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_jobs_get_new(uuid) TO "svc_jobs_worker@postgres";
REVOKE ALL ON FUNCTION pgdbo.f_jobs_get_new(uuid) FROM PUBLIC;

-- Running new Jobs
CREATE OR REPLACE PROCEDURE pgdbo.p_jobs_set_running(IN job_id uuid)
LANGUAGE 'plpgsql'
SECURITY DEFINER
AS $BODY$
DECLARE
    current_status int;
BEGIN
    SELECT "Status" into current_status FROM pgdbo."Jobs" WHERE "Id" = job_id;

    IF current_status = 1 THEN
        RAISE EXCEPTION 'Job is running';
    END IF;

    IF current_status > 1 THEN
        RAISE EXCEPTION 'Job is finished with status %', current_status;
    END IF;

    UPDATE pgdbo."Jobs"
    SET "Status" = 1, "StartedAt" = NOW()
    WHERE "Id" = job_id;
END
$BODY$;

ALTER PROCEDURE pgdbo.p_jobs_set_running(uuid) OWNER TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_jobs_set_running(uuid) TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_jobs_set_running(uuid) TO "svc_jobs_worker@postgres";
REVOKE ALL ON PROCEDURE pgdbo.p_jobs_set_running(uuid) FROM PUBLIC;

-- Saving Jobs results
CREATE OR REPLACE PROCEDURE pgdbo.p_jobs_set_results(IN job_id uuid, IN status integer, IN results bytea)
LANGUAGE 'plpgsql'
SECURITY DEFINER
AS $BODY$
DECLARE
    current_status int;
BEGIN
    IF status < 2 OR status > 5 THEN
        RAISE EXCEPTION 'Status must be >= 2 (Finished) and <= 5 (Lost)';
    END IF;

    SELECT "Status" into current_status FROM pgdbo."Jobs" WHERE "Id" = job_id;

    IF current_status <> 1 THEN
        RAISE EXCEPTION 'Job is not running. Cannot save results';
    END IF;

    UPDATE pgdbo."Jobs"
    SET "Status" = status, "Results" = results, "FinishedAt" = NOW()
    WHERE "Id" = job_id;
END
$BODY$;

ALTER PROCEDURE pgdbo.p_jobs_set_results(uuid, integer, bytea) OWNER TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_jobs_set_results(uuid, integer, bytea) TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_jobs_set_results(uuid, integer, bytea) TO "svc_jobs_worker@postgres";
REVOKE ALL ON PROCEDURE pgdbo.p_jobs_set_results(uuid, integer, bytea) FROM PUBLIC;
