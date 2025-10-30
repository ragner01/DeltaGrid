using System;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using IOC.Core.Domain.Custody;
using Xunit;

namespace IOC.UnitTests;

public class CustodyRoundingTests
{
    [Fact]
    public void Ticket_Hash_Is_Deterministic_For_Same_Payload()
    {
        var now = DateTimeOffset.Parse("2025-01-01T00:00:00Z");
        var t1 = new CustodyTicket(Guid.NewGuid(), "T-100", "M-1", now, now.AddDays(1), 123.456, "user", now);
        var payload = $"{t1.TicketNumber}|{t1.MeterId}|{t1.PeriodStart:o}|{t1.PeriodEnd:o}|{t1.StandardVolume_m3:F3}|{t1.CreatedBy}|{t1.CreatedAt:o}";
        using var sha256 = SHA256.Create();
        var hash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
        t1.SetArtifacts("/artifacts/custody/T-100.pdf", hash);

        var t2 = new CustodyTicket(Guid.NewGuid(), "T-100", "M-1", now, now.AddDays(1), 123.456, "user", now);
        var payload2 = $"{t2.TicketNumber}|{t2.MeterId}|{t2.PeriodStart:o}|{t2.PeriodEnd:o}|{t2.StandardVolume_m3:F3}|{t2.CreatedBy}|{t2.CreatedAt:o}";
        var hash2 = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(payload2))).Replace("-", "").ToLowerInvariant();
        t2.SetArtifacts("/artifacts/custody/T-100.pdf", hash2);

        t1.ImmutableHash.Should().Be(hash2);
    }

    [Fact]
    public void Ticket_Approve_Transitions_From_Pending()
    {
        var now = DateTimeOffset.UtcNow;
        var t = new CustodyTicket(Guid.NewGuid(), "T-101", "M-1", now.AddDays(-1), now, 10.0, "user", now);
        t.Approve();
        t.Status.Should().Be("Approved");
    }
}
