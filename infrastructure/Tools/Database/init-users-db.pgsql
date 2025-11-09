--
-- Creating users
--
CREATE ROLE "svc_jobs_webapp@postgres" WITH
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
CREATE DATABASE "Users"
    WITH
    OWNER = pg_database_owner
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    LOCALE_PROVIDER = 'libc'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;

GRANT ALL ON DATABASE "Users" TO pg_database_owner;
GRANT CONNECT ON DATABASE "Users" TO "svc_jobs_webapp@postgres";

--
-- Creating schema
--
CREATE SCHEMA IF NOT EXISTS pgdbo AUTHORIZATION pg_database_owner;
GRANT ALL ON SCHEMA pgdbo TO pg_database_owner;
GRANT USAGE ON SCHEMA pgdbo TO "svc_jobs_webapp@postgres";

--
-- Creating tables
--
CREATE TABLE IF NOT EXISTS pgdbo."Users"
(
    "Username" text NOT NULL,
    "PasswordHash" text NOT NULL,
    "PasswordSalt" text NOT NULL,
    CONSTRAINT "PK_USERS_USERNAME" PRIMARY KEY ("Username")
)
TABLESPACE pg_default;
ALTER TABLE IF EXISTS pgdbo."Users" OWNER to pg_database_owner;

CREATE TABLE IF NOT EXISTS pgdbo."UsersJobs"
(
    "Username" text NOT NULL,
    "JobId" uuid NOT NULL,
    CONSTRAINT "PK_USERJOBS_USERNAME_JOBID" PRIMARY KEY ("Username", "JobId")
)
TABLESPACE pg_default;
ALTER TABLE IF EXISTS pgdbo."UsersJobs" OWNER to pg_database_owner;

--
-- Creating functions for svc_jobs_webapp
--

-- Adding new Users
CREATE OR REPLACE PROCEDURE pgdbo.p_users_add_new_user(IN username text, IN hash text, IN salt text)
LANGUAGE 'sql'
SECURITY DEFINER
AS $BODY$
    INSERT INTO pgdbo."Users" ("Username", "PasswordHash", "PasswordSalt")
    VALUES (username, hash, salt)
$BODY$;

ALTER PROCEDURE pgdbo.p_users_add_new_user(text, text, text) OWNER TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_users_add_new_user(text, text, text) TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_users_add_new_user(text, text, text) TO "svc_jobs_webapp@postgres";
REVOKE ALL ON PROCEDURE pgdbo.p_users_add_new_user(text, text, text) FROM PUBLIC;

-- Getting User
CREATE OR REPLACE FUNCTION pgdbo.f_users_get_user(username text)
RETURNS TABLE("Username" text, "PasswordHash" text, "PasswordSalt" text)
LANGUAGE 'sql'
SECURITY DEFINER
AS $BODY$
    SELECT "Username", "PasswordHash", "PasswordSalt"
    FROM pgdbo."Users"
    WHERE "Username" = username
$BODY$;

ALTER FUNCTION pgdbo.f_users_get_user(text) OWNER TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user(text) TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user(text) TO "svc_jobs_webapp@postgres";
REVOKE ALL ON FUNCTION pgdbo.f_users_get_user(text) FROM PUBLIC;

-- Add new UserJob
CREATE OR REPLACE PROCEDURE pgdbo.p_users_add_new_job(username text, job_id uuid)
LANGUAGE 'sql'
SECURITY DEFINER
AS $BODY$
    INSERT INTO pgdbo."UsersJobs" ("Username", "JobId")
    VALUES (username, job_id)
$BODY$;

ALTER PROCEDURE pgdbo.p_users_add_new_job(text, uuid) OWNER TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_users_add_new_job(text, uuid) TO pg_database_owner;
GRANT EXECUTE ON PROCEDURE pgdbo.p_users_add_new_job(text, uuid) TO "svc_jobs_webapp@postgres";
REVOKE ALL ON PROCEDURE pgdbo.p_users_add_new_job(text, uuid) FROM PUBLIC;

-- Getting User
CREATE OR REPLACE FUNCTION pgdbo.f_users_get_user_jobs(username text)
RETURNS TABLE("JobId" uuid)
LANGUAGE 'sql'
SECURITY DEFINER
AS $BODY$
    SELECT "JobId"
    FROM pgdbo."UsersJobs"
    WHERE "Username" = username
$BODY$;

ALTER FUNCTION pgdbo.f_users_get_user_jobs(text) OWNER TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user_jobs(text) TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user_jobs(text) TO "svc_jobs_webapp@postgres";
REVOKE ALL ON FUNCTION pgdbo.f_users_get_user_jobs(text) FROM PUBLIC;

-- Checking that Job is belongs to User
CREATE OR REPLACE FUNCTION pgdbo.f_users_check_user_job(username text, job_id uuid)
RETURNS TABLE("JobId" uuid)
LANGUAGE 'sql'
SECURITY DEFINER
AS $BODY$
    SELECT "JobId"
    FROM pgdbo."UsersJobs"
    WHERE "Username" = username AND "JobId" = job_id
$BODY$;

ALTER FUNCTION pgdbo.f_users_get_user_jobs(text) OWNER TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user_jobs(text) TO pg_database_owner;
GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user_jobs(text) TO "svc_jobs_webapp@postgres";
REVOKE ALL ON FUNCTION pgdbo.f_users_get_user_jobs(text) FROM PUBLIC;
