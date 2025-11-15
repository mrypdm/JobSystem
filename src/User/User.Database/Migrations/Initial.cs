using Microsoft.EntityFrameworkCore;
using Shared.Database.Migrations;

namespace User.Database.Migrations;

/// <summary>
/// Migration for testing
/// </summary>
internal sealed class Initial : BaseMigration
{
    /// <inheritdoc />
    public override async Task ApplyAsync(DbContext context, CancellationToken cancellationToken)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE ROLE "svc_users_webapp@postgres" WITH
                LOGIN
                NOSUPERUSER
                NOINHERIT
                NOCREATEDB
                NOCREATEROLE
                NOREPLICATION
                NOBYPASSRLS;
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            GRANT ALL ON DATABASE "Users" TO pg_database_owner;
            GRANT CONNECT ON DATABASE "Users" TO "svc_users_webapp@postgres";
            """, cancellationToken);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE SCHEMA IF NOT EXISTS pgdbo AUTHORIZATION pg_database_owner;
            GRANT ALL ON SCHEMA pgdbo TO pg_database_owner;
            GRANT USAGE ON SCHEMA pgdbo TO "svc_users_webapp@postgres";
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS pgdbo."Users"
            (
                "Username" text NOT NULL,
                "PasswordHash" text NOT NULL,
                "PasswordSalt" text NOT NULL,
                CONSTRAINT "PK_USERS_USERNAME" PRIMARY KEY ("Username")
            )
            TABLESPACE pg_default;
            ALTER TABLE IF EXISTS pgdbo."Users" OWNER to pg_database_owner;
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS pgdbo."UsersJobs"
            (
                "Username" text NOT NULL,
                "JobId" uuid NOT NULL,
                CONSTRAINT "PK_USERJOBS_USERNAME_JOBID" PRIMARY KEY ("Username", "JobId")
            )
            TABLESPACE pg_default;
            ALTER TABLE IF EXISTS pgdbo."UsersJobs" OWNER to pg_database_owner;
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE OR REPLACE PROCEDURE pgdbo.p_users_add_new_user(IN username text, IN hash text, IN salt text)
            LANGUAGE 'sql'
            SECURITY DEFINER
            AS $BODY$
                INSERT INTO pgdbo."Users" ("Username", "PasswordHash", "PasswordSalt")
                VALUES (username, hash, salt)
            $BODY$;

            ALTER PROCEDURE pgdbo.p_users_add_new_user(text, text, text) OWNER TO pg_database_owner;
            GRANT EXECUTE ON PROCEDURE pgdbo.p_users_add_new_user(text, text, text) TO pg_database_owner;
            GRANT EXECUTE ON PROCEDURE pgdbo.p_users_add_new_user(text, text, text) TO "svc_users_webapp@postgres";
            REVOKE ALL ON PROCEDURE pgdbo.p_users_add_new_user(text, text, text) FROM PUBLIC;
            """, cancellationToken);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE OR REPLACE FUNCTION pgdbo.f_users_get_user(IN username text)
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
            GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user(text) TO "svc_users_webapp@postgres";
            REVOKE ALL ON FUNCTION pgdbo.f_users_get_user(text) FROM PUBLIC;
            """, cancellationToken);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE OR REPLACE PROCEDURE pgdbo.p_users_add_new_job(IN username text, IN job_id uuid)
            LANGUAGE 'plpgsql'
            SECURITY DEFINER
            AS $BODY$
            DECLARE
                user_existance boolean;
            BEGIN
                SELECT EXISTS(SELECT 1 FROM pgdbo."Users" WHERE "Username" = username) into user_existance;

                IF NOT user_existance THEN
                    RAISE EXCEPTION 'User % does not exists', username;
                END IF;

                INSERT INTO pgdbo."UsersJobs" ("Username", "JobId")
                VALUES (username, job_id);
            END
            $BODY$;

            ALTER PROCEDURE pgdbo.p_users_add_new_job(text, uuid) OWNER TO pg_database_owner;
            GRANT EXECUTE ON PROCEDURE pgdbo.p_users_add_new_job(text, uuid) TO pg_database_owner;
            GRANT EXECUTE ON PROCEDURE pgdbo.p_users_add_new_job(text, uuid) TO "svc_users_webapp@postgres";
            REVOKE ALL ON PROCEDURE pgdbo.p_users_add_new_job(text, uuid) FROM PUBLIC;
            """, cancellationToken);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE OR REPLACE FUNCTION pgdbo.f_users_get_user_jobs(IN username text)
            RETURNS TABLE("JobId" uuid)
            LANGUAGE 'sql'
            SECURITY DEFINER
            AS $BODY$
                SELECT "JobId"
                FROM pgdbo."Users" U JOIN pgdbo."UsersJobs" UJ ON U."Username" = UJ."Username"
                WHERE U."Username" = username
            $BODY$;

            ALTER FUNCTION pgdbo.f_users_get_user_jobs(text) OWNER TO pg_database_owner;
            GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user_jobs(text) TO pg_database_owner;
            GRANT EXECUTE ON FUNCTION pgdbo.f_users_get_user_jobs(text) TO "svc_users_webapp@postgres";
            REVOKE ALL ON FUNCTION pgdbo.f_users_get_user_jobs(text) FROM PUBLIC;
            """, cancellationToken);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE OR REPLACE FUNCTION pgdbo.f_users_check_user_job(IN username text, IN job_id uuid)
            RETURNS TABLE("Value" boolean)
            LANGUAGE 'sql'
            SECURITY DEFINER
            AS $BODY$
                SELECT EXISTS(
                    SELECT 1
                    FROM pgdbo."Users" U JOIN pgdbo."UsersJobs" UJ ON U."Username" = UJ."Username"
                    WHERE U."Username" = username AND UJ."JobId" = job_id
                )
            $BODY$;

            ALTER FUNCTION pgdbo.f_users_check_user_job(text, uuid) OWNER TO pg_database_owner;
            GRANT EXECUTE ON FUNCTION pgdbo.f_users_check_user_job(text, uuid) TO pg_database_owner;
            GRANT EXECUTE ON FUNCTION pgdbo.f_users_check_user_job(text, uuid) TO "svc_users_webapp@postgres";
            REVOKE ALL ON FUNCTION pgdbo.f_users_check_user_job(text, uuid) FROM PUBLIC;
            """, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task DiscardAsync(DbContext context, CancellationToken cancellationToken)
    {
        await SafeDropSqlAsync(context, "DROP PROCEDURE pgdbo.p_users_add_new_user(text, text, text)", cancellationToken);
        await SafeDropSqlAsync(context, "DROP FUNCTION pgdbo.f_users_get_user(text)", cancellationToken);
        await SafeDropSqlAsync(context, "DROP PROCEDURE pgdbo.p_users_add_new_job(text, uuid)", cancellationToken);
        await SafeDropSqlAsync(context, "DROP FUNCTION pgdbo.f_users_get_user_jobs(text)", cancellationToken);
        await SafeDropSqlAsync(context, "DROP FUNCTION pgdbo.f_users_check_user_job(text, uuid)", cancellationToken);

        await SafeDropSqlAsync(context, "DROP TABLE pgdbo.\"Users\"", cancellationToken);
        await SafeDropSqlAsync(context, "DROP TABLE pgdbo.\"UsersJobs\"", cancellationToken);
        await SafeDropSqlAsync(context, "DROP SCHEMA pgdbo", cancellationToken);

        await SafeDropSqlAsync(context, "REVOKE ALL ON DATABASE \"Users\" FROM \"svc_users_webapp@postgres\"", cancellationToken);
        await SafeDropSqlAsync(context, "DROP ROLE \"svc_users_webapp@postgres\"", cancellationToken);
    }
}
