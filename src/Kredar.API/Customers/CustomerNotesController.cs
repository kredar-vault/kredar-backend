using Kredar.API.Common;
using Kredar.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Customers;

[ApiController]
[Authorize]
[Route("api/v1/customers/{customerId:guid}/notes")]
public class CustomerNotesController(AppDbContext db) : ControllerBase
{
    private Guid TenantId => Guid.Parse(User.FindFirst("tenantId")!.Value);

    [HttpGet]
    public async Task<IActionResult> List(Guid customerId, CancellationToken ct)
    {
        var notes = await db.CustomerNotes
            .Where(n => n.TenantId == TenantId && n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new { n.Id, n.Content, n.CreatedByEmail, n.CreatedAt })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Success(notes));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid customerId, [FromBody] CreateNoteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(ApiResponse<object>.Fail("Content is required."));

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == TenantId, ct);
        if (customer is null) return NotFound(ApiResponse<object>.Fail("Customer not found."));

        var note = new CustomerNote
        {
            TenantId = TenantId,
            CustomerId = customerId,
            Content = req.Content.Trim(),
            CreatedByEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                          ?? User.FindFirst("email")?.Value,
        };
        db.CustomerNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { note.Id, note.Content, note.CreatedByEmail, note.CreatedAt }));
    }

    [HttpPatch("~/api/v1/customers/{customerId2:guid}/notes/{noteId:guid}")]
    public async Task<IActionResult> Update(Guid customerId2, Guid noteId, [FromBody] CreateNoteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(ApiResponse<object>.Fail("Content is required."));

        var note = await db.CustomerNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.CustomerId == customerId2 && n.TenantId == TenantId, ct);
        if (note is null) return NotFound(ApiResponse<object>.Fail("Note not found."));

        note.Content = req.Content.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { note.Id, note.Content, note.CreatedByEmail, note.CreatedAt }));
    }

    [HttpDelete("~/api/v1/notes/{noteId:guid}")]
    public async Task<IActionResult> Delete(Guid noteId, CancellationToken ct)
    {
        var note = await db.CustomerNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.TenantId == TenantId, ct);
        if (note is null) return NotFound(ApiResponse<object>.Fail("Note not found."));
        db.CustomerNotes.Remove(note);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success("Note deleted."));
    }
}

public class CreateNoteRequest
{
    public string Content { get; set; } = string.Empty;
}
