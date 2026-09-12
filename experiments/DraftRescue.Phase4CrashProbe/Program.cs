using DraftRescue.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using DraftRescue.Application.Models;
using DraftRescue.Domain.Drafts;
using DraftRescue.Platform.Windows.Security;

if (args.Length < 1) return 2;
var mode = args[0].ToLowerInvariant();
if (mode != "dpapi" && args.Length < 2) return 2;
var database = args.Length > 1 ? Path.GetFullPath(args[1]) : string.Empty;
switch (mode)
{
    case "dpapi":
        var failureStage = "Unknown";
        var probeDirectory = Path.Combine(Path.GetTempPath(), "DraftRescue-Phase4-DpapiProbe-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(probeDirectory);
            var id = new DraftId(Guid.NewGuid());
            var context = DraftProtectionContext.Create(id, 1);
            var protector = new WindowsDpapiDraftProtector();
            var plaintext = DraftPlaintextPayload.Create("DR_RUNTIME_PROBE_CANARY");
            failureStage = "Protect";
            var protectedPayload = protector.Protect(plaintext, context);
            failureStage = "Unprotect";
            var roundTrip = protector.Unprotect(protectedPayload, context);
            if (roundTrip.Text != plaintext.Text || protectedPayload.Bytes.Length == 0)
            {
                Console.WriteLine("failureStage=Validation");
                return 5;
            }
            Console.WriteLine("payloadRoundTrip=Pass");

            var installationSecretPath = Path.Combine(probeDirectory, "installation-key.protected");
            var installationSecrets = new WindowsDpapiInstallationSecretStore(installationSecretPath);
            failureStage = "InstallationSecretCreate";
            var firstSecret = await installationSecrets.GetOrCreateAsync();
            failureStage = "InstallationSecretLoad";
            var secondSecret = await installationSecrets.GetOrCreateAsync();
            if (!firstSecret.Bytes.Span.SequenceEqual(secondSecret.Bytes.Span))
            {
                Console.WriteLine("failureStage=Validation");
                return 5;
            }
            Console.WriteLine("installationSecretRoundTrip=Pass");
            Console.WriteLine("failureStage=None");
            return 0;
        }
        catch (DraftProtectionException error)
        {
            // Only an audited structural reason is emitted; never print the
            // provider exception message or any payload-derived value.
            Console.WriteLine($"failureStage={failureStage}");
            Console.WriteLine($"failureReason={error.Reason}");
            return 6;
        }
        finally
        {
            try
            {
                if (Directory.Exists(probeDirectory)) Directory.Delete(probeDirectory, recursive: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    case "seed":
        using (var connection = SqliteStoreBootstrapper.Open(database))
        {
            var id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO drafts (draft_id, record_schema_version, application_id, presentation_kind, fingerprint_version, match_metadata_version, match_metadata, protected_payload, protection_version, snapshot_sequence, created_at_utc_ms, updated_at_utc_ms, expires_at_utc_ms, recoverable_state) VALUES ($id,1,'probe',0,1,1,X'01',X'01',1,1,0,0,9999999999999,0);";
            command.Parameters.AddWithValue("$id", id.ToByteArray());
            command.ExecuteNonQuery();
        }
        return 0;
    case "child":
        if (args.Length < 3) return 2;
        var readyPath = Path.GetFullPath(args[2]);
        using (var connection = new SqliteConnection($"Data Source={database};Pooling=false"))
        {
            connection.Open();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE drafts SET protected_payload=X'FF', snapshot_sequence=2 WHERE snapshot_sequence=1;";
            command.ExecuteNonQuery();
            File.WriteAllText(readyPath, "ready");
            Thread.Sleep(TimeSpan.FromMinutes(2));
            transaction.Commit();
        }
        return 0;
    case "verify":
        using (var connection = new SqliteConnection($"Data Source={database};Pooling=false"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT protected_payload, snapshot_sequence FROM drafts LIMIT 1;";
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return 3;
            var payload = (byte[])reader[0];
            var sequence = reader.GetInt64(1);
            Console.WriteLine($"payload={Convert.ToHexString(payload)} sequence={sequence}");
            return payload.Length == 1 && payload[0] == 1 && sequence == 1 ? 0 : 4;
        }
    default:
        return 2;
}
