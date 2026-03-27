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
            IEnumerable<Comment> objCommentList = _unitOfWork.Comment.GetAll(includeProperties: "ApplicationUser,Idea");
            IEnumerable<CommentVM> commentVMList = objCommentList.Select(commentVMItem => new CommentVM()//sử dụng phương thức Select để chuyển đổi mỗi phần tử trong objIdeaList thành một đối tượng IdeaVM mới.
            {
                comment = commentVMItem//gán giá trị của ideaVMItem trong objIdeaList vào thuộc tính idea của IdeaVM.
                
            });
            return View(commentVMList);
        }

        // 2. Action hiển thị Form thêm mới (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 3. Action xử lý dữ liệu thêm mới (POST)
        [HttpPost]
        public IActionResult Create(Comment comment)
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;//lấy thông tin người dùng đang đăng nhập
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//lấy id của người dùng đang đăng nhập
            comment.ApplicationUserId = claim.Value;//gán id của người dùng đang đăng nhập vào thuộc tính ApplicationUserId của shoppingCart
            
            // Kiểm tra tính hợp lệ của dữ liệu
            if (ModelState.IsValid)
            {
                _unitOfWork.Comment.Add(comment); // Thêm vào danh sách chờ
                _unitOfWork.Save();   // Lưu vào SQL Server
                return RedirectToAction("Index"); // Quay về trang danh sách
            }
            return View(comment); // Nếu lỗi (ví dụ chưa nhập ngày), hiện lại form
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)//nếu id null hoặc = 0 thì trả về notfound (không tìm thấy).
            {
                return NotFound();
            }
            //var categoryfromDb = _db.Categories.Find(id);//tạo biến var categoryfromDb và cho = find id để tìm tới id của nó trong database
            var commentfromDbFirst = _unitOfWork.Comment.GetFirstOrDefault(u => u.Id == id);
            //var categoryfromDbsingle = _db.Categories.SingleOrDefault(u => u.Id == id);
            if (commentfromDbFirst == null)//ở đây do đã set categoryfromDb = id nên id null hoặc = 0 thì categoryfromDb cũng null và trả về notfound giống id.
            {
                return NotFound();
            }
            return View(commentfromDbFirst);//trả về view dù cho có đáp ứng 2 điều kiện trên hay không.
        }

        //post
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Edit(Comment obj)
        {
            //if (obj.Name == obj.DisplayOrder.ToString())
            //{
            //    ModelState.AddModelError("name", "The Name must not same displayorder");
            //}
            var claimsIdentity = (ClaimsIdentity)User.Identity;//lấy thông tin người dùng đang đăng nhập
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//lấy id của người dùng đang đăng nhập
            obj.ApplicationUserId = claim.Value;//gán id của người dùng đang đăng nhập vào thuộc tính ApplicationUserId của shoppingCart

            if (ModelState.IsValid)
            {
                _unitOfWork.Comment.Update(obj);
                _unitOfWork.Save();
                TempData["Sucess"] = "Category Edit sucessfully";
                return RedirectToAction("index");
            }
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //var categoryfromDb = _db.Categories.Find(id);
            var topicfromDbFirst = _unitOfWork.Topic.GetFirstOrDefault(u => u.Id == id);
            //var categoryfromDbsingle = _db.Categories.SingleOrDefault(u => u.Id == id);
            if (topicfromDbFirst == null)
            {
                return NotFound();
            }
            return View(topicfromDbFirst);

        }

        [HttpPost]
        public IActionResult DeletePost(int? id)
        {
            // 1. Lấy thông tin Idea cần xóa từ CSDL
            var obj = _unitOfWork.Comment.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            else
            {
                // 4. Sau khi các dữ liệu liên quan đã bị xóa, ta có thể xóa Idea một cách an toàn
                _unitOfWork.Comment.Remove(obj);
                // 5. Lưu toàn bộ thay đổi xuống Cơ sở dữ liệu
                
            }

            _unitOfWork.Save();

            // Thông báo thành công và chuyển hướng về trang danh sách
            TempData["success"] = "Đã xóa Comment thành công!";
            return RedirectToAction("Index");

        }
    }
}
