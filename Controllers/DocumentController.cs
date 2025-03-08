using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SubdivisionWebsite.Controllers
{
    public class DocumentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DocumentController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Document
        public async Task<IActionResult> Index(DocumentType? type = null)
        {
            IQueryable<Document> documentsQuery = _context.Documents
                .Include(d => d.UploadedBy)
                .Where(d => d.IsPublic);

            if (type.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.DocumentType == type.Value);
            }

            var documents = await documentsQuery
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.SelectedType = type;
            ViewBag.DocumentTypes = Enum.GetValues(typeof(DocumentType))
                .Cast<DocumentType>()
                .Select(t => new { Value = (int)t, Text = t.ToString() })
                .ToList();

            return View(documents);
        }

        // GET: Document/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }

        // GET: Document/Create
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Create()
        {
            ViewBag.DocumentTypes = Enum.GetValues(typeof(DocumentType))
                .Cast<DocumentType>()
                .Select(t => new { Value = (int)t, Text = t.ToString() })
                .ToList();

            return View();
        }

        // POST: Document/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create(Document document, IFormFile documentFile)
        {
            ViewBag.DocumentTypes = Enum.GetValues(typeof(DocumentType))
                .Cast<DocumentType>()
                .Select(t => new { Value = (int)t, Text = t.ToString() })
                .ToList();

            if (documentFile == null || documentFile.Length == 0)
            {
                ModelState.AddModelError("documentFile", "Please select a file to upload.");
                return View(document);
            }

            if (ModelState.IsValid)
            {
                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(documentFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await documentFile.CopyToAsync(fileStream);
                }

                // Set document properties
                document.FilePath = $"/uploads/documents/{uniqueFileName}";
                document.FileType = Path.GetExtension(documentFile.FileName).TrimStart('.');
                document.FileSize = documentFile.Length;
                document.UploadedById = GetCurrentUserId();
                document.UploadedAt = DateTime.UtcNow;
                document.Version = 1;

                _context.Add(document);
                await _context.SaveChangesAsync();

                // Create notifications for users
                await CreateDocumentNotification(document);

                return RedirectToAction(nameof(Index));
            }
            return View(document);
        }

        // GET: Document/Edit/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            ViewBag.DocumentTypes = Enum.GetValues(typeof(DocumentType))
                .Cast<DocumentType>()
                .Select(t => new { Value = (int)t, Text = t.ToString() })
                .ToList();

            return View(document);
        }

        // POST: Document/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int id, Document document, IFormFile documentFile)
        {
            if (id != document.Id)
            {
                return NotFound();
            }

            ViewBag.DocumentTypes = Enum.GetValues(typeof(DocumentType))
                .Cast<DocumentType>()
                .Select(t => new { Value = (int)t, Text = t.ToString() })
                .ToList();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDocument = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
                    if (existingDocument == null)
                    {
                        return NotFound();
                    }

                    // If a new file is uploaded, update the file
                    if (documentFile != null && documentFile.Length > 0)
                    {
                        // Create uploads directory if it doesn't exist
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Generate unique filename
                        var uniqueFileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(documentFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await documentFile.CopyToAsync(fileStream);
                        }

                        // Update document properties
                        document.FilePath = $"/uploads/documents/{uniqueFileName}";
                        document.FileType = Path.GetExtension(documentFile.FileName).TrimStart('.');
                        document.FileSize = documentFile.Length;
                        document.Version = existingDocument.Version + 1;
                    }
                    else
                    {
                        // Keep existing file information
                        document.FilePath = existingDocument.FilePath;
                        document.FileType = existingDocument.FileType;
                        document.FileSize = existingDocument.FileSize;
                        document.Version = existingDocument.Version;
                    }

                    document.UploadedById = existingDocument.UploadedById;
                    document.UploadedAt = existingDocument.UploadedAt;
                    document.LastUpdatedById = GetCurrentUserId();
                    document.LastUpdatedAt = DateTime.UtcNow;

                    _context.Update(document);
                    await _context.SaveChangesAsync();

                    // Create notifications for document update
                    if (documentFile != null && documentFile.Length > 0)
                    {
                        await CreateDocumentUpdateNotification(document);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentExists(document.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(document);
        }

        // GET: Document/Download/5
        public async Task<IActionResult> Download(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            // Increment download count
            document.DownloadCount++;
            _context.Update(document);
            await _context.SaveChangesAsync();

            // Get the file path
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            
            // Check if file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Return the file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", Path.GetFileName(document.FilePath));
        }

        // Helper methods
        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.Id == id);
        }

        private string GetCurrentUserId()
        {
            return _userManager.GetUserId(User) ?? throw new InvalidOperationException("User is not authenticated");
        }

        private async Task CreateDocumentNotification(Document document)
        {
            // Create notification for all users
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var notification = new Notification
                {
                    Title = "New Document Available",
                    Message = $"A new document '{document.Title}' has been uploaded.",
                    UserId = user.Id,
                    Type = NotificationType.Document,
                    ReferenceId = document.Id,
                    ActionUrl = $"/Document/Details/{document.Id}"
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        private async Task CreateDocumentUpdateNotification(Document document)
        {
            // Create notification for all users
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var notification = new Notification
                {
                    Title = "Document Updated",
                    Message = $"The document '{document.Title}' has been updated to version {document.Version}.",
                    UserId = user.Id,
                    Type = NotificationType.Document,
                    ReferenceId = document.Id,
                    ActionUrl = $"/Document/Details/{document.Id}"
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }
    }
} 