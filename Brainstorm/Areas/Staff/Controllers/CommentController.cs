using Brainstorm.DataAccess.Repository.IRepository;
using Brainstorm.Models;
using Brainstorm.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;

namespace Brainstorm.Areas.Staff.Controllers
{
    [Area("Staff")]
    public class CommentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        // Constructor: Nhận ApplicationDbContext để thao tác với CSDL
        public CommentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // 1. Action hiển thị danh sách Topic
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

    }
}
