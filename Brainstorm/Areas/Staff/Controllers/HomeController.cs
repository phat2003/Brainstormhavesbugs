using Brainstorm.DataAccess.Repository;
using Brainstorm.DataAccess.Repository.IRepository;
using Brainstorm.Models;
using Brainstorm.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Diagnostics;
using System.Security.Claims;

namespace Brainstorm.Areas.Staff.Controllers
{
    [Area("Staff")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;//biến này chỉ được đọc(không được ghi hay làm gì khác).
        private IWebHostEnvironment _webHostEnvironment;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        
        
        public IActionResult Index(int id)
        {

            IEnumerable<Idea> objIdeaList = _unitOfWork.Idea.GetAll(includeProperties: "Category,Topic,ApplicationUser");//chỗ này buộc phải có includeProperties để lấy dữ liệu từ bảng Category và Topic liên kết với bảng Idea. Nếu không View này sẽ báo lỗi null.
            IEnumerable<View> objViewList = _unitOfWork.View.GetAll(includeProperties: "ApplicationUser,Idea");//chỗ này buộc phải có includeProperties để lấy dữ liệu từ bảng Category và Topic liên kết với bảng Idea. Nếu không View này sẽ báo lỗi null.
            IEnumerable<React> objReactList = _unitOfWork.React.GetAll(includeProperties: "ApplicationUser,Idea");//chỗ này buộc phải có includeProperties để lấy dữ liệu từ bảng Category và Topic liên kết với bảng Idea. Nếu không View này sẽ báo lỗi null.
            // --- THÊM ĐOẠN NÀY ĐỂ HIỂN THỊ DATA CỦA CÁC MODEL KHÁC LÊN VIEWS TRONG ViewModel ---
            IEnumerable<IdeaVM> ideaVMList = objIdeaList.Select(ideaVMItem => new IdeaVM()//sử dụng phương thức Select để chuyển đổi mỗi phần tử trong objIdeaList thành một đối tượng IdeaVM mới.
            {
                idea = ideaVMItem,//gán giá trị của ideaVMItem trong objIdeaList vào thuộc tính idea của IdeaVM.
                view = objViewList.FirstOrDefault(v => v.IdeaId == ideaVMItem.Id),//lấy view có IdeaId trùng với Id của ideaVMItem trong objIdeaList. Nếu không tìm thấy sẽ trả về null.
                react = objReactList.FirstOrDefault(r => r.IdeaId == ideaVMItem.Id),//lấy react có IdeaId trùng với Id của ideaVMItem trong objIdeaList. Nếu không tìm thấy sẽ trả về null.

                // Thêm 2 dòng đếm số lượng này vào:
                LikeCount = objReactList.Count(r => r.IdeaId == ideaVMItem.Id && r.ReactValue == 1),//đếm số lượng react có IdeaId trùng với Id của ideaVMItem trong objIdeaList và có ReactValue bằng 1 (Like).
                DislikeCount = objReactList.Count(r => r.IdeaId == ideaVMItem.Id && r.ReactValue == -1),//đếm số lượng react có IdeaId trùng với Id của ideaVMItem trong objIdeaList và có ReactValue bằng -1 (Dislike).

                // Thêm dòng này để tính tổng lượt xem:
                ViewCount = objViewList.Where(v => v.IdeaId == ideaVMItem.Id).Sum(v => v.VisitTime)//tính tổng số lượt xem bằng cách lọc các view có IdeaId trùng với Id của ideaVMItem trong objIdeaList và sau đó tính tổng giá trị VisitTime của chúng.
            });

            // 3. Trả danh sách ViewModel về cho View
            return View(ideaVMList);
        }
        public IActionResult Upsert(int? id)
        {
            IdeaVM ideaVM = new IdeaVM();
            ideaVM.idea = new Idea();
            ideaVM.CategoryList = _unitOfWork.Category.GetAll().Select(
                u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }
                );
            ideaVM.TopicList = _unitOfWork.Topic.GetAll().Select(
                u => new SelectListItem { Text = u.Name, Value = u.Id.ToString() }
                );

            if (id == null || id == 0)//nếu id null hoặc = 0 thì trả về notfound (không tìm thấy).
            {
                //Create product
                return View(ideaVM);//mỗi lần tạo sản phẩm xong là return về View là product
            }
            else
            {
                //Update product
                ideaVM.idea = _unitOfWork.Idea.GetFirstOrDefault(u => u.Id == id);//lấy data sản phẩm có sẵn của bảng product đã thêm vào database trước đó có id trùng với id truyền vào.
                //lúc này productVM.product sẽ có dữ liệu của sản phẩm cần update chứ vẫn chưa update vì đây là action get để lấy dữ liệu từ database và show dữ liệu ra thôi.
                //action httppost sẽ làm nhiệm vụ update dữ liệu.


            }


            return View(ideaVM);//trả về view dù cho có đáp ứng 2 điều kiện trên hay không.

        }
        [HttpPost]
        [Authorize]
        public IActionResult Upsert(IdeaVM obj, IFormFile? filepath)
        {
            if (ModelState.IsValid)
            {
                //upload images
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (filepath != null)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(wwwRootPath, @"images\ideas");
                    var extension = Path.GetExtension(filepath.FileName);
                    if (obj.idea.FilePath != null)
                    {
                        //this is an edit and we need to remove old image
                        var oldImagePath = Path.Combine(wwwRootPath, obj.idea.FilePath.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        filepath.CopyTo(fileStreams);
                    }
                    obj.idea.FilePath = @"images\ideas\" + fileName + extension;

                }
                //obj.idea.Id = 0;
                var claimsIdentity = (ClaimsIdentity)User.Identity;//lấy thông tin người dùng đang đăng nhập
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//lấy id của người dùng đang đăng nhập
                obj.idea.ApplicationUserId = claim.Value;//gán id của người dùng đang đăng nhập vào thuộc tính ApplicationUserId của shoppingCart


                if (obj.idea.Id == 0)
                {
                    _unitOfWork.Idea.Add(obj.idea);
                }
                else
                {
                    _unitOfWork.Idea.Update(obj.idea);
                }

                _unitOfWork.Save();
                TempData["Sucess"] = "Product create sucessfully";
                return RedirectToAction("index");
            }

            // --- THÊM ĐOẠN NÀY ĐỂ FIX LỖI MẤT DROPDOWN KHI CÓ LỖI ---
            obj.CategoryList = _unitOfWork.Category.GetAll().Select(
                u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
            obj.TopicList = _unitOfWork.Topic.GetAll().Select(
                u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
            // --------------------------------------------------------

            return View(obj);
        }

        //public IActionResult ViewDetails(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    var ideaFromDbFirst = _unitOfWork.Idea.GetFirstOrDefault(u => u.Id == id, includeProperties: "Category,Topic");
        //    if (ideaFromDbFirst == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(ideaFromDbFirst);
        //}

        [Authorize]
        public IActionResult Views(int id)
        {
            
            View viewObj = new()
            {
                Idea = _unitOfWork.Idea.GetFirstOrDefault(u => u.Id == id, includeProperties: "Category,Topic,ApplicationUser"),
                VisitTime = 1,
                IdeaId = id
            };

            if (id == null || id == 0)
            {
                return NotFound();
            }
            if (viewObj == null)
            {
                return NotFound();
            }

            return View(viewObj);



        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]//chức năng này chỉ dành cho người dùng đăng nhập
        public IActionResult Views(View view)
        {
            view.Id = 0;
            var claimsIdentity = (ClaimsIdentity)User.Identity;//lấy thông tin người dùng đang đăng nhập
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//lấy id của người dùng đang đăng nhập
            view.ApplicationUserId = claim.Value;//gán id của người dùng đang đăng nhập vào thuộc tính ApplicationUserId của shoppingCart

            View viewObj = _unitOfWork.View.GetFirstOrDefault(u => u.ApplicationUserId == claim.Value && u.IdeaId == view.IdeaId);//kiểm tra xem sản phẩm đã tồn tại trong giỏ hàng của người dùng chưa

            if (viewObj == null)
            {
                _unitOfWork.View.Add(view);//nếu sản phẩm chưa tồn tại trong giỏ hàng của người dùng thì thêm sản phẩm vào giỏ hàng
            }
            else
            {
                _unitOfWork.View.IncrementCount(viewObj, view.VisitTime);//nếu sản phẩm đã tồn tại trong giỏ hàng của người dùng thì tăng số lượng sản phẩm trong giỏ hàng lên shoppingCart.Count

            }


            _unitOfWork.Save();//lưu thay đổi vào database

            return RedirectToAction("Views", new { id = view.IdeaId });


        }

        [HttpPost]
        [Authorize] // Chỉ cho phép người dùng đã đăng nhập thực hiện Like/Dislike
        public IActionResult ReactToIdea(React react, int ideaId, int reactValue)
        {
            // 1. Lấy thông tin ID của người dùng đang đăng nhập
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            //string userId = claim.Value;
            react.ApplicationUserId = claim.Value;

            // 2. Tìm xem người dùng này đã từng React ý tưởng này trong Database chưa
            var existingReact = _unitOfWork.React.GetFirstOrDefault(
                r => r.IdeaId == ideaId && r.ApplicationUserId == react.ApplicationUserId
            );

            if (existingReact == null)
            {
                // TRƯỜNG HỢP A: Chưa từng tương tác -> Tạo mới
                React newReact = new React
                {
                    IdeaId = ideaId,
                    ApplicationUserId = react.ApplicationUserId,
                    ReactValue = reactValue
                };
                _unitOfWork.React.Add(newReact);
            }
            else
            {
                // TRƯỜNG HỢP B: Đã từng tương tác
                if (existingReact.ReactValue == reactValue)
                {
                    // Bấm lại đúng nút cũ -> Xóa tương tác (Bỏ Like/Bỏ Dislike)
                    _unitOfWork.React.Remove(existingReact);
                }
                else
                {
                    // Đổi ý (Từ Like sang Dislike hoặc ngược lại) -> Cập nhật giá trị mới
                    existingReact.ReactValue = reactValue;
                    _unitOfWork.React.Update(existingReact);
                }
            }

            // 3. Lưu các thay đổi vào Cơ sở dữ liệu
            _unitOfWork.Save();

            // 4. Tạm thời load lại trang Index sau khi xử lý xong
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
