
using E_Commerce.Models;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Data.Entity;
using System.Net;
using System.Data.Entity.Validation;

namespace E_Commerce.Controllers
{
    public class UsersController : Controller
    {
        E_CommerceEntities DB = new E_CommerceEntities();

        // GET: Users
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login([Bind(Include = "Email, Password")] User user)
        {
            if (user == null)
            {
                ViewBag.ErrorMessage = "Please Enter Your Email And Password.";
                return View();
            }

            var existingUser = DB.Users.SingleOrDefault(u => u.Email == user.Email);

            if (existingUser != null)
            {
                if (existingUser.Password == user.Password)
                {
                    // Store user information in session
                    Session["UserId"] = existingUser.ID;
                    Session["UserEmail"] = existingUser.Email;
                    Session["UserName"] = existingUser.Name;
                    Session["islogin"] = true;


                    // Redirect to the home page or desired page
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.ErrorMessage = "Password is incorrect";
                    Session["islogin"] = false;
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Email not found";
            }

            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Include = "Name , Email , Password")] User user)
        {
            if (user == null)
            {
                ViewBag.ErrorMessage = "Data not found or matched";
            }
            else
            {
                DB.Users.Add(user);
                DB.SaveChanges();
            }
            return RedirectToAction("Login");
        }

        public ActionResult Index()
        {
            var categories = DB.Categories.ToList();
            return View(categories);
        }

        public ActionResult Products(int id)
        {
            var products = DB.Products.Where(p => p.CategoryID == id).ToList();

            return View(products);
        }
        public ActionResult Logout()
        {
            Session["islogin"] = false;
            return RedirectToAction("Index");
        }

        public ActionResult ShowDetails(int id)
        {
            var details = DB.Products.FirstOrDefault(p => p.ID == id);
            return View(details);

        }

        
        public ActionResult AddCart(int productId)
        {
            if (Session["islogin"] is bool isLoggedIn && isLoggedIn)
            {
                var userId = (int)Session["UserId"];

                var cart = DB.Carts.FirstOrDefault(x => x.UserID == userId);
                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserID = userId,
                        CreatedAt = DateTime.Now
                    };
                    DB.Carts.Add(cart);
                    DB.SaveChanges();
                }

                var productImg = GetProductImageUrl(productId); 

                var cartItem = DB.ShoppingCartItems.FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId);

                if (cartItem == null)
                {
                    cartItem = new ShoppingCartItem
                    {
                        CartID = cart.CartID,
                        ProductID = productId,
                        CreatedAt = DateTime.Now,
                        Quantity = 1,
                        Img = productImg 
                    };
                    DB.ShoppingCartItems.Add(cartItem);
                }
                else
                {
                    cartItem.Quantity += 1;
                    cartItem.Img = productImg;
                }

                DB.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        private string GetProductImageUrl(int productId)
        {
            
            var product = DB.Products.FirstOrDefault(p => p.ID == productId);
            return product?.Image_URL; 
        }

        public ActionResult Cart()
        {
            var userId = Session["UserId"] as int?;
            if (userId == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var shoppingCart = DB.Carts.FirstOrDefault(m => m.UserID == userId);
            if (shoppingCart == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var shoppingCartItems = DB.ShoppingCartItems
                .Include(x => x.Product)
                .Where(m => m.CartID == shoppingCart.CartID)
                .ToList();

            var totalAmount = shoppingCartItems.Sum(item => item.Quantity * item.Product.Price);

            ViewBag.ShoppingCartItems = shoppingCartItems;
            ViewBag.TotalAmount = totalAmount;

            return View(); 
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int id, string operation)
        {
            var item = DB.ShoppingCartItems.Where(model => model.ProductID == id).FirstOrDefault();

            if (item != null)
            {
                if (operation == "increase")
                {
                    item.Quantity++;
                }
                else if (operation == "decrease" && item.Quantity > 1)
                {
                    item.Quantity--;
                }

                DB.SaveChanges();
            }

            return RedirectToAction("Cart");
        }
        [HttpPost]
        public ActionResult DeleteItem(int id)
        {
            var item = DB.ShoppingCartItems.SingleOrDefault(i => i.ProductID == id);

            if (item != null)
            {
                DB.ShoppingCartItems.Remove(item);
                DB.SaveChanges();
            }

            return RedirectToAction("Cart");
        }
        public ActionResult Profile()
        {
            var userId = Session["UserId"] as int?;
            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = DB.Users.FirstOrDefault(u => u.ID == userId);
            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }
        public ActionResult EditProfile(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = DB.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile([Bind(Include = "ID,Name,Email,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = DB.Users.Find(user.ID);
                    if (existingUser == null)
                    {
                        return HttpNotFound();
                    }

                    existingUser.Name = user.Name;
                    existingUser.Email = user.Email;

                    
                    if (!string.IsNullOrEmpty(user.Password))
                    {
                        existingUser.Password = user.Password; 
                    }

                    DB.Entry(existingUser).State = EntityState.Modified;
                    DB.SaveChanges();

                    return RedirectToAction("Profile");
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred: " + ex.Message);
                }
            }

            return View(user);
        }


    }

}



