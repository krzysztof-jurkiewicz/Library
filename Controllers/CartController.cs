using System.Data;
using System.Text.Json;
using LibraryFinal.Models;
using LibraryFinal.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace LibraryFinal.Controllers
{
    public class CartController: Controller
    {
        private const string ItemsList = "ItemsList";
        private readonly IBookRepository _bookRepository;
        private readonly IOrderRepository _orderRepository;

        public CartController(IBookRepository bookRepository, IOrderRepository orderRepository)
        {
            _bookRepository = bookRepository;
            _orderRepository = orderRepository;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var sessionItems = HttpContext.Session.GetString(ItemsList);
            var items = string.IsNullOrEmpty(sessionItems)
                ? Enumerable.Empty<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(sessionItems);

            return View(items);
        }

        [HttpPost]
        public IActionResult AddItem(int itemId)
        {
            var book = GetBook(itemId);

            if (book == null)
                return BadRequest();

            var sessionItems = HttpContext.Session.GetString(ItemsList);
            var items = string.IsNullOrEmpty(sessionItems)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(sessionItems);

            var cartItem = items!.FirstOrDefault(i => i.Id == book.Id);

            if (cartItem == null)
            {
                items!.Add(new CartItem()
                {
                    Title = book.Title,
                    Pages = book.Pages,
                    Count = 1,
                    Id = book.Id
                });
            }
            else
            {
                cartItem.Count += 1;
            }

            var serializedItems = JsonSerializer.Serialize(items);
            HttpContext.Session.SetString(ItemsList, serializedItems);

            return Ok(cartItem);
        }

        private Book GetBook(int itemId)
        {
            return _bookRepository.GetBook(itemId);
        }

        [HttpPost]
        public IActionResult DeleteItem(int itemId)
        {
            var book = _bookRepository.GetBook(itemId);

            if (book == null)
                return NotFound();

            var sessionItems = HttpContext.Session.GetString(ItemsList);

            if (string.IsNullOrEmpty(sessionItems))
                return BadRequest();

            var items = JsonSerializer.Deserialize<List<CartItem>>(sessionItems);

            var cartItem = items!.FirstOrDefault(i => i.Id == itemId);

            if (cartItem == null)
                return BadRequest();

            if (cartItem.Count > 0)
                cartItem.Count -= 1;

            if (cartItem.Count == 0)
                items!.Remove(cartItem);

            var serializedItems = JsonSerializer.Serialize(items);
            HttpContext.Session.SetString(ItemsList, serializedItems);

            return Ok(cartItem);
        }

        public IActionResult CreateOrder()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateOrder([Bind("ReaderCardNumber,Phone,Address,Id,Date")] Order order)
        {
            ModelState.Remove("OrderBooks");

            if (!ModelState.IsValid)
            {
                return View(order);
            }

            order.Id = Guid.NewGuid();
            order.Date = DateTime.Now;

            var items = JsonSerializer.Deserialize<List<CartItem>>(HttpContext.Session.GetString(ItemsList));

            order.OrderBooks = items.Select(i => new OrderBook { OrderId = order.Id, BookId = i.Id, Count = i.Count }).ToList();

            _orderRepository.SaveOrder(order);

            return View("PlacedOrder", order);
        }
    }
}
