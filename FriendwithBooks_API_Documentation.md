# FriendwithBooks Backend API Documentation

## Table of Contents

1. [Authentication](#authentication)
2. [Base URL](#base-url)
3. [API Endpoints](#api-endpoints)
   - [Authentication Controller](#authentication-controller)
   - [Book Controller](#book-controller)
   - [Cart Controller](#cart-controller)
   - [Order Controller](#order-controller)
   - [Payment Controller](#payment-controller)
   - [Admin Controller](#admin-controller)
   - [Home Controller](#home-controller)
   - [Profile Controller](#profile-controller)
   - [Chat Controller](#chat-controller)
   - [SignalR Chat Hub](#signalr-chat-hub)

## Authentication

The API uses JWT (JSON Web Token) for authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your_jwt_token>
```

### JWT Token Claims

- `userId`: User ID
- `fullName`: User's full name
- `email`: User's email
- `phone`: User's phone number
- `address`: User's address
- `avatar`: User's avatar URL
- `registrationDate`: User's registration date
- `role`: User's role (user/admin)

## Base URL

```
https://your-domain.com/api
```

---

## API Endpoints

### Authentication Controller

#### 1. Register User

- **URL**: `POST /api/Auth/register`
- **Description**: Register a new user account
- **Authentication**: Not required
- **Request Body**:

```json
{
  "fullName": "string",
  "email": "string",
  "password": "string",
  "phone": "string (optional)",
  "address": "string (optional)"
}
```

- **Response**:

```json
{
  "token": "jwt_token_string"
}
```

#### 2. Login User

- **URL**: `POST /api/Auth/login`
- **Description**: Authenticate user and get JWT token
- **Authentication**: Not required
- **Request Body**:

```json
{
  "email": "string",
  "password": "string"
}
```

- **Response**:

```json
{
  "token": "jwt_token_string"
}
```

#### 3. Google Login

- **URL**: `POST /api/Auth/googleLogin`
- **Description**: Login with Google account
- **Authentication**: Not required
- **Request Body**:

```json
{
  "email": "string",
  "fullName": "string"
}
```

- **Response**:

```json
{
  "token": "jwt_token_string"
}
```

#### 4. Update Profile

- **URL**: `PUT /api/Auth/updateProfile`
- **Description**: Update user profile information
- **Authentication**: Required
- **Request Body**:

```json
{
  "fullName": "string",
  "phone": "string (optional)",
  "address": "string (optional)",
  "avatar": "string (optional)"
}
```

- **Response**:

```json
{
  "token": "jwt_token_string"
}
```

#### 5. Forgot Password

- **URL**: `POST /api/Auth/forgotPassword`
- **Description**: Reset user password
- **Authentication**: Not required
- **Request Body**:

```json
{
  "email": "string",
  "password": "string"
}
```

- **Response**:

```json
{
  "token": "jwt_token_string"
}
```

### Book Controller

#### 1. Get Book by ID

- **URL**: `GET /api/Book/{id}`
- **Description**: Get detailed book information with category path and related books
- **Authentication**: Not required
- **Response**:

```json
{
  "book": {
    "bookID": 1,
    "title": "string",
    "author": "string",
    "description": "string",
    "price": 100000,
    "stock": 50,
    "imgURL1": "string",
    "imgURL2": "string",
    "imgURL3": "string",
    "ageGroup": "string",
    "avgRating": 4.5,
    "totalRating": 100,
    "categoryID": 1,
    "supplier": "string",
    "publishYear": "2023-01-01T00:00:00Z",
    "language": "string",
    "pageNum": "200",
    "binding": "string",
    "discount": 10,
    "isFlashSale": true
  },
  "categoryPath": [
    {
      "categoryID": 1,
      "categoryName": "string"
    }
  ],
  "relatedBooks": [
    {
      "bookID": 2,
      "title": "string",
      "author": "string",
      "price": 100000,
      "discount": 10,
      "imgURL1": "string",
      "flashSale": true
    }
  ]
}
```

#### 2. Get All Categories

- **URL**: `GET /api/Book/category`
- **Description**: Get all book categories with subcategories
- **Authentication**: Not required
- **Response**:

```json
{
  "Parent Category": [
    {
      "name": "string",
      "totalStock": 100,
      "categoryID": 1
    }
  ]
}
```

#### 3. Create Category

- **URL**: `POST /api/Book/category`
- **Description**: Create a new category
- **Authentication**: Required
- **Request Body**:

```json
{
  "categoryName": "string",
  "parentName": "string (optional)"
}
```

#### 4. Update Category

- **URL**: `PUT /api/Book/category/{id}`
- **Description**: Update category information
- **Authentication**: Required
- **Request Body**:

```json
{
  "newName": "string",
  "newParentName": "string (optional)"
}
```

#### 5. Delete Category

- **URL**: `DELETE /api/Book/category/{name}`
- **Description**: Delete a category
- **Authentication**: Required

#### 6. Search Books

- **URL**: `GET /api/Book/query`
- **Description**: Search and filter books
- **Authentication**: Not required
- **Query Parameters**:
  - `page` (int, default: 1): Page number
  - `promo` (bool, optional): Filter by promotion
  - `price` (string, optional): Price filter
  - `priceMin` (string, optional): Minimum price
  - `priceMax` (string, optional): Maximum price
  - `age` (string, optional): Age group filter
  - `type` (string, optional): Book type filter
  - `category` (int, optional): Category ID filter
  - `name` (string, optional): Book name search

#### 7. Get Book Reviews

- **URL**: `GET /api/Book/{id}/reviews`
- **Description**: Get reviews for a specific book
- **Authentication**: Not required

#### 8. Add Review

- **URL**: `PUT /api/Book/addReview`
- **Description**: Add a review for a book
- **Authentication**: Required
- **Request Body**:

```json
{
  "bookID": 1,
  "rating": 5,
  "comment": "string"
}
```

#### 9. Admin Book Search

- **URL**: `GET /api/Book/admin/query`
- **Description**: Admin interface for searching books
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `page` (int, default: 1): Page number
  - `pageSize` (int, default: 12): Items per page
  - `title` (string, optional): Book title search
  - `id` (int, optional): Book ID
  - `promo` (bool, optional): Promotion filter
  - `price` (string, optional): Price filter
  - `age` (string, optional): Age group filter
  - `type` (string, optional): Book type filter
  - `categoryId` (int, optional): Category ID filter

### Cart Controller

#### 1. Get My Cart

- **URL**: `GET /api/Cart/my`
- **Description**: Get current user's cart items
- **Authentication**: Required
- **Response**:

```json
{
  "success": true,
  "data": {
    "userID": 1,
    "items": [
      {
        "cartID": 1,
        "bookID": 1,
        "quantity": 2,
        "createDate": "2023-01-01T00:00:00Z",
        "book": {
          "bookID": 1,
          "title": "string",
          "author": "string",
          "price": 100000,
          "discount": 10,
          "stock": 50,
          "imgURL1": "string",
          "discountedPrice": 90000
        }
      }
    ],
    "totalItems": 2,
    "totalAmount": 180000,
    "maxItemsPerCart": 20,
    "maxQuantityPerItem": 10
  }
}
```

#### 2. Add to Cart

- **URL**: `POST /api/Cart`
- **Description**: Add item to cart
- **Authentication**: Required
- **Request Body**:

```json
{
  "bookID": 1,
  "quantity": 2
}
```

#### 3. Update Cart Item

- **URL**: `PUT /api/Cart/{cartId}`
- **Description**: Update quantity of cart item
- **Authentication**: Required
- **Request Body**:

```json
{
  "bookID": 1,
  "quantity": 3
}
```

#### 4. Remove from Cart

- **URL**: `DELETE /api/Cart/{cartId}`
- **Description**: Remove item from cart
- **Authentication**: Required

#### 5. Clear Cart

- **URL**: `DELETE /api/Cart/clear`
- **Description**: Remove all items from cart
- **Authentication**: Required

#### 6. Get Cart Count

- **URL**: `GET /api/Cart/count`
- **Description**: Get total number of items in cart
- **Authentication**: Required
- **Response**:

```json
{
  "success": true,
  "count": 5
}
```

### Order Controller

#### 1. Get My Orders

- **URL**: `GET /api/Order/my`
- **Description**: Get current user's orders
- **Authentication**: Required
- **Response**:

```json
{
  "success": true,
  "data": [
    {
      "orderID": 1,
      "orderDate": "2023-01-01T00:00:00Z",
      "totalAmount": 180000,
      "status": "Pending",
      "paymentMethod": "string",
      "paymentStatus": "Pending",
      "itemCount": 2,
      "canCancel": true,
      "orderDetails": [
        {
          "orderDetailID": 1,
          "bookID": 1,
          "quantity": 2,
          "unitPrice": 90000,
          "book": {
            "title": "string",
            "author": "string",
            "imgURL1": "string"
          }
        }
      ]
    }
  ]
}
```

#### 2. Get Order Details

- **URL**: `GET /api/Order/{orderId}`
- **Description**: Get detailed order information
- **Authentication**: Required

#### 3. Create Order

- **URL**: `POST /api/Order`
- **Description**: Create new order from cart
- **Authentication**: Required
- **Request Body**:

```json
{
  "paymentMethodId": 1
}
```

#### 4. Cancel Order

- **URL**: `PUT /api/Order/{orderId}/cancel`
- **Description**: Cancel an order
- **Authentication**: Required

### Payment Controller

#### 1. Get Payment Methods

- **URL**: `GET /api/Payment/methods`
- **Description**: Get all available payment methods
- **Authentication**: Required
- **Response**:

```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "string",
      "img": "string"
    }
  ]
}
```

#### 2. Create Payment Method

- **URL**: `POST /api/Payment/method`
- **Description**: Create new payment method (Admin only)
- **Authentication**: Required
- **Request Body**:

```json
{
  "methodName": "string"
}
```

#### 3. Process Payment

- **URL**: `POST /api/Payment/process/{orderId}`
- **Description**: Process payment for an order
- **Authentication**: Required

### Admin Controller

#### Product Management

##### 1. Get All Products

- **URL**: `GET /api/admin/products`
- **Description**: Get all products with pagination
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `page` (int, default: 1): Page number
  - `pageSize` (int, default: 20): Items per page

##### 2. Get Product by ID

- **URL**: `GET /api/admin/products/{id}`
- **Description**: Get specific product details
- **Authentication**: Required (Admin)

##### 3. Create Product

- **URL**: `POST /api/admin/products`
- **Description**: Create new product
- **Authentication**: Required (Admin)
- **Request Body**:

```json
{
  "title": "string",
  "author": "string",
  "description": "string",
  "price": 100000,
  "stock": 50,
  "categoryID": 1,
  "discount": 10,
  "imgURL1": "string",
  "imgURL2": "string",
  "imgURL3": "string",
  "supplier": "string",
  "publishYear": 2023,
  "pageNum": 200,
  "language": "string",
  "binding": "string",
  "ageGroup": "string"
}
```

##### 4. Update Product

- **URL**: `PUT /api/admin/products/{id}`
- **Description**: Update product information
- **Authentication**: Required (Admin)

##### 5. Delete Product

- **URL**: `DELETE /api/admin/products/{id}`
- **Description**: Delete product
- **Authentication**: Required (Admin)

#### Order Management

##### 1. Get All Orders

- **URL**: `GET /api/admin/orders`
- **Description**: Get all orders with pagination
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `page` (int, default: 1): Page number
  - `pageSize` (int, default: 20): Items per page
  - `status` (string, optional): Order status filter

##### 2. Get Order by ID

- **URL**: `GET /api/admin/orders/{id}`
- **Description**: Get specific order details
- **Authentication**: Required (Admin)

##### 3. Update Order

- **URL**: `PUT /api/admin/orders/{id}`
- **Description**: Update order information
- **Authentication**: Required (Admin)

##### 4. Update Order Status

- **URL**: `PUT /api/admin/orders/{id}/status`
- **Description**: Update order status
- **Authentication**: Required (Admin)
- **Request Body**:

```json
{
  "status": "string",
  "note": "string (optional)"
}
```

##### 5. Delete Order

- **URL**: `DELETE /api/admin/orders/{id}`
- **Description**: Delete order
- **Authentication**: Required (Admin)

##### 6. Search Orders

- **URL**: `GET /api/admin/orders/search`
- **Description**: Search orders with filters
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `searchTerm` (string, optional): Search term
  - `status` (string, default: "all"): Status filter
  - `dateFrom` (DateTime, optional): Start date
  - `dateTo` (DateTime, optional): End date
  - `minAmount` (decimal, optional): Minimum amount
  - `maxAmount` (decimal, optional): Maximum amount
  - `page` (int, default: 1): Page number
  - `pageSize` (int, default: 20): Items per page

##### 7. Bulk Update Order Status

- **URL**: `POST /api/admin/orders/bulk-update`
- **Description**: Update multiple orders status
- **Authentication**: Required (Admin)
- **Request Body**:

```json
{
  "orderIds": [1, 2, 3],
  "status": "string",
  "note": "string (optional)"
}
```

#### User Management

##### 1. Get All Users

- **URL**: `GET /api/admin/users`
- **Description**: Get all users with pagination
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `page` (int, default: 1): Page number
  - `pageSize` (int, default: 20): Items per page

##### 2. Get User by ID

- **URL**: `GET /api/admin/users/{id}`
- **Description**: Get specific user details
- **Authentication**: Required (Admin)

##### 3. Delete User

- **URL**: `DELETE /api/admin/users/{id}`
- **Description**: Delete user
- **Authentication**: Required (Admin)

##### 4. Update User Role

- **URL**: `PUT /api/admin/users/{id}/role`
- **Description**: Update user role
- **Authentication**: Required (Admin)
- **Request Body**:

```json
{
  "role": "string"
}
```

#### Statistics

##### 1. Sales Statistics

- **URL**: `GET /api/admin/statistics/sales`
- **Description**: Get sales statistics
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `period` (string, default: "month"): Time period

##### 2. Product Statistics

- **URL**: `GET /api/admin/statistics/products`
- **Description**: Get product statistics
- **Authentication**: Required (Admin)

##### 3. Revenue Statistics

- **URL**: `GET /api/admin/statistics/revenue`
- **Description**: Get revenue statistics
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `startTime` (string, optional): Start time
  - `endTime` (string, optional): End time

##### 4. Chart Points

- **URL**: `GET /api/admin/statistics/chartpoints`
- **Description**: Get chart data points
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `period` (string, optional): Time period
  - `startTime` (string, optional): Start time
  - `endTime` (string, optional): End time

##### 5. Top 5 Books

- **URL**: `GET /api/admin/statistics/top5books`
- **Description**: Get top 5 selling books
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `startTime` (string, optional): Start time
  - `endTime` (string, optional): End time

##### 6. Top 5 Categories

- **URL**: `GET /api/admin/statistics/top5categories`
- **Description**: Get top 5 selling categories
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `startTime` (string, optional): Start time
  - `endTime` (string, optional): End time

##### 7. User Statistics

- **URL**: `GET /api/admin/statistics/users`
- **Description**: Get user statistics
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `period` (string, default: "month"): Time period

##### 8. Latest Orders

- **URL**: `GET /api/admin/statistics/latestorders`
- **Description**: Get latest orders
- **Authentication**: Required (Admin)

##### 9. Order Completion Rate

- **URL**: `GET /api/admin/statistics/order-completion-rate`
- **Description**: Get order completion rate
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `startTime` (string, optional): Start time
  - `endTime` (string, optional): End time

##### 10. Top 10 Customers

- **URL**: `GET /api/admin/statistics/top10customers`
- **Description**: Get top 10 customers
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `startTime` (string, optional): Start time
  - `endTime` (string, optional): End time

#### Flash Sale Management

##### 1. Get All Flash Sales

- **URL**: `GET /api/admin/flash-sale`
- **Description**: Get all flash sales
- **Authentication**: Required (Admin)

##### 2. Get Flash Sales by Book ID

- **URL**: `GET /api/admin/flash-sale/book/{bookId}`
- **Description**: Get flash sales for specific book
- **Authentication**: Required (Admin)

##### 3. Create Flash Sale

- **URL**: `POST /api/admin/flash-sale`
- **Description**: Create new flash sale
- **Authentication**: Required (Admin)
- **Request Body**:

```json
{
  "bookID": 1,
  "discountPercent": 20,
  "startTime": "2023-01-01T00:00:00Z",
  "endTime": "2023-01-02T00:00:00Z"
}
```

##### 4. Update Flash Sale

- **URL**: `PUT /api/admin/flash-sale/{id}`
- **Description**: Update flash sale
- **Authentication**: Required (Admin)
- **Request Body**:

```json
{
  "discountPercent": 20,
  "startTime": "2023-01-01T00:00:00Z",
  "endTime": "2023-01-02T00:00:00Z"
}
```

##### 5. Delete Flash Sale

- **URL**: `DELETE /api/admin/flash-sale/{id}`
- **Description**: Delete flash sale
- **Authentication**: Required (Admin)

##### 6. Get Active Flash Sales

- **URL**: `GET /api/admin/flash-sale/active`
- **Description**: Get currently active flash sales
- **Authentication**: Required (Admin)

### Home Controller

#### 1. Get Best Sellers

- **URL**: `GET /api/Home/BestSellers`
- **Description**: Get best selling books
- **Authentication**: Not required
- **Response**:

```json
[
  {
    "bookID": 1,
    "title": "string",
    "author": "string",
    "description": "string",
    "imgURL": "string"
  }
]
```

#### 2. Get Flash Sale

- **URL**: `GET /api/Home/FlashSale`
- **Description**: Get current flash sale items
- **Authentication**: Not required
- **Response**:

```json
[
  {
    "bookID": 1,
    "title": "string",
    "price": 100000,
    "author": "string",
    "imgURL": "string",
    "discountPercent": 20,
    "startTime": "2023-01-01T00:00:00Z",
    "endTime": "2023-01-02T00:00:00Z"
  }
]
```

### Profile Controller

#### 1. Get Profile

- **URL**: `GET /api/Profile`
- **Description**: Get current user's profile information
- **Authentication**: Required
- **Response**:

```json
{
  "fullName": "string",
  "email": "string",
  "phoneNumber": "string",
  "address": "string",
  "avatar": "string",
  "role": "string",
  "registrationDate": "2023-01-01T00:00:00Z"
}
```

### Chat Controller

#### 1. Get Conversations

- **URL**: `GET /api/Chat/conversations`
- **Description**: Get conversation history
- **Authentication**: Required
- **Query Parameters**:
  - `page` (int, default: 1): Page number
  - `partnerId` (int, default: 152): Partner user ID

#### 2. Get Latest Messages

- **URL**: `GET /api/Chat/conversations/latest`
- **Description**: Get latest messages since last message ID
- **Authentication**: Required
- **Query Parameters**:
  - `lastMessageId` (string): Last message ID
  - `partnerId` (int, default: 152): Partner user ID

#### 3. Get Chat Partners

- **URL**: `GET /api/Chat/conversations/partners`
- **Description**: Get list of chat partners
- **Authentication**: Required

#### 4. Delete Conversation

- **URL**: `DELETE /api/Chat/message`
- **Description**: Delete conversation with partner
- **Authentication**: Required (Admin)
- **Query Parameters**:
  - `partnerId` (int): Partner user ID

### SignalR Chat Hub

#### Hub URL

```
/api/chathub
```

#### Methods

##### Send Message

- **Method**: `SendMessage`
- **Parameters**:
  - `message` (string): Message content
  - `recvID` (int): Receiver user ID
- **Description**: Send a message to another user

#### Connection Events

##### On Connected

- **Event**: `OnConnectedAsync`
- **Description**: Triggered when user connects to hub

##### On Disconnected

- **Event**: `OnDisconnectedAsync`
- **Description**: Triggered when user disconnects from hub

#### Authentication

The SignalR hub requires JWT authentication. Include the token as a query parameter:

```
/api/chathub?access_token=<your_jwt_token>
```

---

## Error Responses

### Common Error Formats

#### 400 Bad Request

```json
{
  "success": false,
  "message": "Error description"
}
```

#### 401 Unauthorized

```json
{
  "success": false,
  "message": "Authentication required"
}
```

#### 403 Forbidden

```json
{
  "success": false,
  "message": "Access denied"
}
```

#### 404 Not Found

```json
{
  "success": false,
  "message": "Resource not found"
}
```

#### 500 Internal Server Error

```json
{
  "success": false,
  "message": "Internal server error",
  "error": "Error details"
}
```

## Data Models

### User Model

```json
{
  "userID": 1,
  "fullName": "string",
  "email": "string",
  "password": "string (hashed)",
  "phone": "string",
  "address": "string",
  "avatar": "string",
  "registrationDate": "2023-01-01T00:00:00Z",
  "role": "string"
}
```

### Book Model

```json
{
  "bookID": 1,
  "title": "string",
  "author": "string",
  "description": "string",
  "price": 100000,
  "stock": 50,
  "discount": 10,
  "imgURL1": "string",
  "imgURL2": "string",
  "imgURL3": "string",
  "ageGroup": "string",
  "avgRating": 4.5,
  "totalRating": 100,
  "categoryID": 1,
  "supplier": "string",
  "publishYear": "2023-01-01T00:00:00Z",
  "language": "string",
  "pageNum": "200",
  "binding": "string"
}
```

### Order Model

```json
{
  "orderID": 1,
  "userID": 1,
  "orderDate": "2023-01-01T00:00:00Z",
  "totalAmount": 180000,
  "status": "string",
  "paymentMethodID": 1
}
```

### Cart Model

```json
{
  "cartID": 1,
  "userID": 1,
  "bookID": 1,
  "quantity": 2,
  "createDate": "2023-01-01T00:00:00Z"
}
```

## Database

The application uses PostgreSQL as the database with Entity Framework Core as the ORM.

## Caching

The application uses in-memory caching for:

- Best seller data
- Flash sale data

Cache keys:

- `BestSellerData`
- `FlashSaleData`

## Security Features

1. **JWT Authentication**: Secure token-based authentication
2. **Password Hashing**: SHA256 hashing for passwords
3. **Role-based Authorization**: User and Admin roles
4. **Input Validation**: Model validation and business rule validation
5. **SQL Injection Protection**: Entity Framework Core parameterized queries

## Development Notes

- The application uses .NET 8.0
- Swagger/OpenAPI documentation is available in development mode
- SignalR is used for real-time chat functionality
- Google authentication is supported
- The application includes comprehensive error handling and logging
