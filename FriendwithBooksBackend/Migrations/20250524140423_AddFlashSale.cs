using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendwithBooksBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_Categories_CategoryID",
                table: "Books");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Books_BookID",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Users_UserID",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_FlashSales_Books_BookID",
                table: "FlashSales");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Books_BookID",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Orders_OrderID",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentMethods_PaymentMethodID",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserID",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Books_BookID",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_UserID",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Orders_OrderID",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reviews",
                table: "Reviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentMethods",
                table: "PaymentMethods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderDetails",
                table: "OrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FlashSales",
                table: "FlashSales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Carts",
                table: "Carts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Books",
                table: "Books");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "transactions");

            migrationBuilder.RenameTable(
                name: "Reviews",
                newName: "reviews");

            migrationBuilder.RenameTable(
                name: "PaymentMethods",
                newName: "paymentmethods");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "orders");

            migrationBuilder.RenameTable(
                name: "OrderDetails",
                newName: "orderdetails");

            migrationBuilder.RenameTable(
                name: "FlashSales",
                newName: "flashsales");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "categories");

            migrationBuilder.RenameTable(
                name: "Carts",
                newName: "carts");

            migrationBuilder.RenameTable(
                name: "Books",
                newName: "books");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "users",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "RegistrationDate",
                table: "users",
                newName: "registrationdate");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "users",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "users",
                newName: "password");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "users",
                newName: "fullname");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Avatar",
                table: "users",
                newName: "avatar");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "users",
                newName: "userid");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "transactions",
                newName: "paymentstatus");

            migrationBuilder.RenameColumn(
                name: "PaymentDate",
                table: "transactions",
                newName: "paymentdate");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "transactions",
                newName: "orderid");

            migrationBuilder.RenameColumn(
                name: "TransactionID",
                table: "transactions",
                newName: "transactionid");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_OrderID",
                table: "transactions",
                newName: "IX_transactions_orderid");

            migrationBuilder.RenameColumn(
                name: "ReviewDate",
                table: "reviews",
                newName: "reviewdate");

            migrationBuilder.RenameColumn(
                name: "Rating",
                table: "reviews",
                newName: "rating");

            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "reviews",
                newName: "comment");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "reviews",
                newName: "userid");

            migrationBuilder.RenameColumn(
                name: "BookID",
                table: "reviews",
                newName: "bookid");

            migrationBuilder.RenameColumn(
                name: "ReviewID",
                table: "reviews",
                newName: "reviewid");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_UserID",
                table: "reviews",
                newName: "IX_reviews_userid");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_BookID",
                table: "reviews",
                newName: "IX_reviews_bookid");

            migrationBuilder.RenameColumn(
                name: "MethodName",
                table: "paymentmethods",
                newName: "methodname");

            migrationBuilder.RenameColumn(
                name: "PaymentMethodID",
                table: "paymentmethods",
                newName: "paymentmethodid");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "orders",
                newName: "userid");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "orders",
                newName: "totalamount");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "orders",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "PaymentMethodID",
                table: "orders",
                newName: "paymentmethodid");

            migrationBuilder.RenameColumn(
                name: "OrderDate",
                table: "orders",
                newName: "orderdate");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "orders",
                newName: "orderid");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_UserID",
                table: "orders",
                newName: "IX_orders_userid");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_PaymentMethodID",
                table: "orders",
                newName: "IX_orders_paymentmethodid");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "orderdetails",
                newName: "unitprice");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "orderdetails",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "BookID",
                table: "orderdetails",
                newName: "bookid");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "orderdetails",
                newName: "orderid");

            migrationBuilder.RenameColumn(
                name: "OrderDetailID",
                table: "orderdetails",
                newName: "orderdetailid");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetails_OrderID",
                table: "orderdetails",
                newName: "IX_orderdetails_orderid");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetails_BookID",
                table: "orderdetails",
                newName: "IX_orderdetails_bookid");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "flashsales",
                newName: "starttime");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "flashsales",
                newName: "endtime");

            migrationBuilder.RenameColumn(
                name: "DiscountPercent",
                table: "flashsales",
                newName: "discountpercent");

            migrationBuilder.RenameColumn(
                name: "BookID",
                table: "flashsales",
                newName: "bookid");

            migrationBuilder.RenameColumn(
                name: "FlashSaleID",
                table: "flashsales",
                newName: "flashsaleid");

            migrationBuilder.RenameIndex(
                name: "IX_FlashSales_BookID",
                table: "flashsales",
                newName: "IX_flashsales_bookid");

            migrationBuilder.RenameColumn(
                name: "ParentID",
                table: "categories",
                newName: "parentid");

            migrationBuilder.RenameColumn(
                name: "CategoryName",
                table: "categories",
                newName: "categoryname");

            migrationBuilder.RenameColumn(
                name: "CategoryID",
                table: "categories",
                newName: "categoryid");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "carts",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "carts",
                newName: "createdate");

            migrationBuilder.RenameColumn(
                name: "BookID",
                table: "carts",
                newName: "bookid");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "carts",
                newName: "userid");

            migrationBuilder.RenameColumn(
                name: "CartID",
                table: "carts",
                newName: "cartid");

            migrationBuilder.RenameIndex(
                name: "IX_Carts_UserID",
                table: "carts",
                newName: "IX_carts_userid");

            migrationBuilder.RenameIndex(
                name: "IX_Carts_BookID",
                table: "carts",
                newName: "IX_carts_bookid");

            migrationBuilder.RenameColumn(
                name: "TotalRating",
                table: "books",
                newName: "totalrating");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "books",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Supplier",
                table: "books",
                newName: "supplier");

            migrationBuilder.RenameColumn(
                name: "Stock",
                table: "books",
                newName: "stock");

            migrationBuilder.RenameColumn(
                name: "PublishYear",
                table: "books",
                newName: "publishyear");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "books",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "PageNum",
                table: "books",
                newName: "pagenum");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "books",
                newName: "language");

            migrationBuilder.RenameColumn(
                name: "ImgURL",
                table: "books",
                newName: "imgurl");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "books",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "CategoryID",
                table: "books",
                newName: "categoryid");

            migrationBuilder.RenameColumn(
                name: "Binding",
                table: "books",
                newName: "binding");

            migrationBuilder.RenameColumn(
                name: "AvgRating",
                table: "books",
                newName: "avgrating");

            migrationBuilder.RenameColumn(
                name: "Author",
                table: "books",
                newName: "author");

            migrationBuilder.RenameColumn(
                name: "BookID",
                table: "books",
                newName: "bookid");

            migrationBuilder.RenameIndex(
                name: "IX_Books_CategoryID",
                table: "books",
                newName: "IX_books_categoryid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "userid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transactions",
                table: "transactions",
                column: "transactionid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reviews",
                table: "reviews",
                columns: new[] { "reviewid", "bookid", "userid" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_paymentmethods",
                table: "paymentmethods",
                column: "paymentmethodid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_orders",
                table: "orders",
                column: "orderid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_orderdetails",
                table: "orderdetails",
                columns: new[] { "orderdetailid", "orderid", "bookid" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_flashsales",
                table: "flashsales",
                column: "flashsaleid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_categories",
                table: "categories",
                column: "categoryid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_carts",
                table: "carts",
                columns: new[] { "cartid", "userid", "bookid" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_books",
                table: "books",
                column: "bookid");

            migrationBuilder.AddForeignKey(
                name: "FK_books_categories_categoryid",
                table: "books",
                column: "categoryid",
                principalTable: "categories",
                principalColumn: "categoryid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_carts_books_bookid",
                table: "carts",
                column: "bookid",
                principalTable: "books",
                principalColumn: "bookid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_carts_users_userid",
                table: "carts",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_flashsales_books_bookid",
                table: "flashsales",
                column: "bookid",
                principalTable: "books",
                principalColumn: "bookid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orderdetails_books_bookid",
                table: "orderdetails",
                column: "bookid",
                principalTable: "books",
                principalColumn: "bookid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orderdetails_orders_orderid",
                table: "orderdetails",
                column: "orderid",
                principalTable: "orders",
                principalColumn: "orderid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_paymentmethods_paymentmethodid",
                table: "orders",
                column: "paymentmethodid",
                principalTable: "paymentmethods",
                principalColumn: "paymentmethodid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_users_userid",
                table: "orders",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_books_bookid",
                table: "reviews",
                column: "bookid",
                principalTable: "books",
                principalColumn: "bookid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_users_userid",
                table: "reviews",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_orders_orderid",
                table: "transactions",
                column: "orderid",
                principalTable: "orders",
                principalColumn: "orderid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_books_categories_categoryid",
                table: "books");

            migrationBuilder.DropForeignKey(
                name: "FK_carts_books_bookid",
                table: "carts");

            migrationBuilder.DropForeignKey(
                name: "FK_carts_users_userid",
                table: "carts");

            migrationBuilder.DropForeignKey(
                name: "FK_flashsales_books_bookid",
                table: "flashsales");

            migrationBuilder.DropForeignKey(
                name: "FK_orderdetails_books_bookid",
                table: "orderdetails");

            migrationBuilder.DropForeignKey(
                name: "FK_orderdetails_orders_orderid",
                table: "orderdetails");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_paymentmethods_paymentmethodid",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_users_userid",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_books_bookid",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_users_userid",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_orders_orderid",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transactions",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reviews",
                table: "reviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_paymentmethods",
                table: "paymentmethods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_orders",
                table: "orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_orderdetails",
                table: "orderdetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_flashsales",
                table: "flashsales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_categories",
                table: "categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_carts",
                table: "carts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_books",
                table: "books");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "transactions",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "reviews",
                newName: "Reviews");

            migrationBuilder.RenameTable(
                name: "paymentmethods",
                newName: "PaymentMethods");

            migrationBuilder.RenameTable(
                name: "orders",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "orderdetails",
                newName: "OrderDetails");

            migrationBuilder.RenameTable(
                name: "flashsales",
                newName: "FlashSales");

            migrationBuilder.RenameTable(
                name: "categories",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "carts",
                newName: "Carts");

            migrationBuilder.RenameTable(
                name: "books",
                newName: "Books");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "registrationdate",
                table: "Users",
                newName: "RegistrationDate");

            migrationBuilder.RenameColumn(
                name: "phone",
                table: "Users",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "Users",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "fullname",
                table: "Users",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "avatar",
                table: "Users",
                newName: "Avatar");

            migrationBuilder.RenameColumn(
                name: "userid",
                table: "Users",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "paymentstatus",
                table: "Transactions",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "paymentdate",
                table: "Transactions",
                newName: "PaymentDate");

            migrationBuilder.RenameColumn(
                name: "orderid",
                table: "Transactions",
                newName: "OrderID");

            migrationBuilder.RenameColumn(
                name: "transactionid",
                table: "Transactions",
                newName: "TransactionID");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_orderid",
                table: "Transactions",
                newName: "IX_Transactions_OrderID");

            migrationBuilder.RenameColumn(
                name: "reviewdate",
                table: "Reviews",
                newName: "ReviewDate");

            migrationBuilder.RenameColumn(
                name: "rating",
                table: "Reviews",
                newName: "Rating");

            migrationBuilder.RenameColumn(
                name: "comment",
                table: "Reviews",
                newName: "Comment");

            migrationBuilder.RenameColumn(
                name: "userid",
                table: "Reviews",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "bookid",
                table: "Reviews",
                newName: "BookID");

            migrationBuilder.RenameColumn(
                name: "reviewid",
                table: "Reviews",
                newName: "ReviewID");

            migrationBuilder.RenameIndex(
                name: "IX_reviews_userid",
                table: "Reviews",
                newName: "IX_Reviews_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_reviews_bookid",
                table: "Reviews",
                newName: "IX_Reviews_BookID");

            migrationBuilder.RenameColumn(
                name: "methodname",
                table: "PaymentMethods",
                newName: "MethodName");

            migrationBuilder.RenameColumn(
                name: "paymentmethodid",
                table: "PaymentMethods",
                newName: "PaymentMethodID");

            migrationBuilder.RenameColumn(
                name: "userid",
                table: "Orders",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "totalamount",
                table: "Orders",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Orders",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "paymentmethodid",
                table: "Orders",
                newName: "PaymentMethodID");

            migrationBuilder.RenameColumn(
                name: "orderdate",
                table: "Orders",
                newName: "OrderDate");

            migrationBuilder.RenameColumn(
                name: "orderid",
                table: "Orders",
                newName: "OrderID");

            migrationBuilder.RenameIndex(
                name: "IX_orders_userid",
                table: "Orders",
                newName: "IX_Orders_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_orders_paymentmethodid",
                table: "Orders",
                newName: "IX_Orders_PaymentMethodID");

            migrationBuilder.RenameColumn(
                name: "unitprice",
                table: "OrderDetails",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "OrderDetails",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "bookid",
                table: "OrderDetails",
                newName: "BookID");

            migrationBuilder.RenameColumn(
                name: "orderid",
                table: "OrderDetails",
                newName: "OrderID");

            migrationBuilder.RenameColumn(
                name: "orderdetailid",
                table: "OrderDetails",
                newName: "OrderDetailID");

            migrationBuilder.RenameIndex(
                name: "IX_orderdetails_orderid",
                table: "OrderDetails",
                newName: "IX_OrderDetails_OrderID");

            migrationBuilder.RenameIndex(
                name: "IX_orderdetails_bookid",
                table: "OrderDetails",
                newName: "IX_OrderDetails_BookID");

            migrationBuilder.RenameColumn(
                name: "starttime",
                table: "FlashSales",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "endtime",
                table: "FlashSales",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "discountpercent",
                table: "FlashSales",
                newName: "DiscountPercent");

            migrationBuilder.RenameColumn(
                name: "bookid",
                table: "FlashSales",
                newName: "BookID");

            migrationBuilder.RenameColumn(
                name: "flashsaleid",
                table: "FlashSales",
                newName: "FlashSaleID");

            migrationBuilder.RenameIndex(
                name: "IX_flashsales_bookid",
                table: "FlashSales",
                newName: "IX_FlashSales_BookID");

            migrationBuilder.RenameColumn(
                name: "parentid",
                table: "Categories",
                newName: "ParentID");

            migrationBuilder.RenameColumn(
                name: "categoryname",
                table: "Categories",
                newName: "CategoryName");

            migrationBuilder.RenameColumn(
                name: "categoryid",
                table: "Categories",
                newName: "CategoryID");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "Carts",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "createdate",
                table: "Carts",
                newName: "CreateDate");

            migrationBuilder.RenameColumn(
                name: "bookid",
                table: "Carts",
                newName: "BookID");

            migrationBuilder.RenameColumn(
                name: "userid",
                table: "Carts",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "cartid",
                table: "Carts",
                newName: "CartID");

            migrationBuilder.RenameIndex(
                name: "IX_carts_userid",
                table: "Carts",
                newName: "IX_Carts_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_carts_bookid",
                table: "Carts",
                newName: "IX_Carts_BookID");

            migrationBuilder.RenameColumn(
                name: "totalrating",
                table: "Books",
                newName: "TotalRating");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Books",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "supplier",
                table: "Books",
                newName: "Supplier");

            migrationBuilder.RenameColumn(
                name: "stock",
                table: "Books",
                newName: "Stock");

            migrationBuilder.RenameColumn(
                name: "publishyear",
                table: "Books",
                newName: "PublishYear");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "Books",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "pagenum",
                table: "Books",
                newName: "PageNum");

            migrationBuilder.RenameColumn(
                name: "language",
                table: "Books",
                newName: "Language");

            migrationBuilder.RenameColumn(
                name: "imgurl",
                table: "Books",
                newName: "ImgURL");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Books",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "categoryid",
                table: "Books",
                newName: "CategoryID");

            migrationBuilder.RenameColumn(
                name: "binding",
                table: "Books",
                newName: "Binding");

            migrationBuilder.RenameColumn(
                name: "avgrating",
                table: "Books",
                newName: "AvgRating");

            migrationBuilder.RenameColumn(
                name: "author",
                table: "Books",
                newName: "Author");

            migrationBuilder.RenameColumn(
                name: "bookid",
                table: "Books",
                newName: "BookID");

            migrationBuilder.RenameIndex(
                name: "IX_books_categoryid",
                table: "Books",
                newName: "IX_Books_CategoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "TransactionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reviews",
                table: "Reviews",
                columns: new[] { "ReviewID", "BookID", "UserID" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentMethods",
                table: "PaymentMethods",
                column: "PaymentMethodID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "OrderID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderDetails",
                table: "OrderDetails",
                columns: new[] { "OrderDetailID", "OrderID", "BookID" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_FlashSales",
                table: "FlashSales",
                column: "FlashSaleID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "CategoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Carts",
                table: "Carts",
                columns: new[] { "CartID", "UserID", "BookID" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Books",
                table: "Books",
                column: "BookID");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Categories_CategoryID",
                table: "Books",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Books_BookID",
                table: "Carts",
                column: "BookID",
                principalTable: "Books",
                principalColumn: "BookID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Users_UserID",
                table: "Carts",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FlashSales_Books_BookID",
                table: "FlashSales",
                column: "BookID",
                principalTable: "Books",
                principalColumn: "BookID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Books_BookID",
                table: "OrderDetails",
                column: "BookID",
                principalTable: "Books",
                principalColumn: "BookID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Orders_OrderID",
                table: "OrderDetails",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "OrderID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentMethods_PaymentMethodID",
                table: "Orders",
                column: "PaymentMethodID",
                principalTable: "PaymentMethods",
                principalColumn: "PaymentMethodID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserID",
                table: "Orders",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Books_BookID",
                table: "Reviews",
                column: "BookID",
                principalTable: "Books",
                principalColumn: "BookID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_UserID",
                table: "Reviews",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Orders_OrderID",
                table: "Transactions",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "OrderID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
