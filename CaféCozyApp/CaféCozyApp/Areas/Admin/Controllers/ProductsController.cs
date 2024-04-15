﻿using CaféCozyApp.Areas.Admin.ViewModels.Product;
using CaféCozyApp.Areas.Admin.ViewModels.Slider;
using CaféCozyApp.Data;
using CaféCozyApp.Helpers;
using CaféCozyApp.Models;
using CaféCozyApp.Services;
using CaféCozyApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace CaféCozyApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public ProductsController(AppDbContext context, IWebHostEnvironment environment, IProductService productService, ICategoryService categoryService)
        {
            _context = context;
            _env = environment;
            _productService = productService;
            _categoryService = categoryService;
        }



        public async Task<IActionResult> Index()
        {
            return View(await _productService.GetAll());
        }


        public async Task<IActionResult> Create()
        {
            ViewBag.ProductCategories = await GetCategoryAsync();
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductCreateVM model)
        {
            try
            {
                ViewBag.ProductCategories = await GetCategoryAsync();

                if (!model.Photo.CheckFileType("image/"))
                {
                    ModelState.AddModelError("Photo", "File type must be image");
                    return View(model);
                }

                if (!model.Photo.CheckFileSize(2097152))
                {
                    ModelState.AddModelError("Photo", "Image size must be max 2 MB");
                    return View(model);

                }

                string fileName = Guid.NewGuid().ToString() + " " + model.Photo.FileName;
                string newPath = FileHelper.GetFilePath(_env.WebRootPath, "uploads/products", fileName);
                await FileHelper.SaveFileAsync(newPath, model.Photo);

                Product newProduct = new()
                {
                    ImageUrl = fileName,
                    Description = model.Description,
                    Name = model.Name,
                    Price = model.Price,
                    CategoryId = model.CategoryId,
                };


                await _context.Products.AddAsync(newProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));


            }
            catch (Exception ex)
            {

                ViewBag.error = ex.Message;
                return View();
            }

        }



        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                ViewBag.ProductCategories = await GetCategoryAsync();


                if (id == null) return BadRequest();
                Product dbProduct = await _productService.GetById(id);
                if (dbProduct == null) return NotFound();

                ProductUpdateVM model = new()
                {
                    ImageUrl = dbProduct.ImageUrl,
                    Description = dbProduct.Description,
                    Name = dbProduct.Name,
                    Price = dbProduct.Price,
                    CategoryId = dbProduct.CategoryId,

                };
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return View();
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, ProductUpdateVM updatedProduct)
        {
            try
            {

                ViewBag.ProductCategories = await GetCategoryAsync();

                if (id == null) return BadRequest();
                Product dbProduct = await _productService.GetById(id);
                if (dbProduct == null) return NotFound();

                ProductUpdateVM model = new()
                {
                    ImageUrl = dbProduct.ImageUrl,
                    Name = dbProduct.Name,
                    Description = dbProduct.Description,
                    Price = dbProduct.Price,
                    CategoryId = dbProduct.CategoryId
                };
                if (updatedProduct.Photo != null)
                {
                    if (!updatedProduct.Photo.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photo", "File type must be image");
                        return View(model);
                    }
                    if (!updatedProduct.Photo.CheckFileSize(2097152))
                    {
                        ModelState.AddModelError("Photo", "Image size must be max 2 MB");
                        return View(model);
                    }

                    string deletePath = FileHelper.GetFilePath(_env.WebRootPath, "uploads/products", dbProduct.ImageUrl);
                    FileHelper.DeleteFile(deletePath);
                    string fileName = Guid.NewGuid().ToString() + " " + updatedProduct.Photo.FileName;
                    string newPath = FileHelper.GetFilePath(_env.WebRootPath, "uploads/products", fileName);
                    await FileHelper.SaveFileAsync(newPath, updatedProduct.Photo);
                    dbProduct.ImageUrl = fileName;
                }
                else
                {
                    Product newProduct = new()
                    {
                        ImageUrl = dbProduct.ImageUrl
                    };
                }

                dbProduct.Name = updatedProduct.Name;
                dbProduct.Description = updatedProduct.Description;
                dbProduct.Price = updatedProduct.Price;
                dbProduct.CategoryId = updatedProduct.CategoryId;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return View();
            }
        }




        private async Task<SelectList> GetCategoryAsync()
        {
            List<ProductCategory> categories = await _categoryService.GetAll();
            return new SelectList(categories, "Id", "Name");
        }



    }
}
