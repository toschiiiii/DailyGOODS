# DailyGOODS

A grocery POS and inventory system for Windows, built with VB.NET Windows Forms and SQLite.
Inspired by the "DailyGOODS Grocery Management System" capstone paper and rebuilt from scratch.

## Features

- Role-based login (Admin and Cashier) with hashed passwords
- Cashier register: barcode entry, quantity controls, PWD and Senior discounts (5%),
  cash and change, membership points, receipt print preview
- Keyboard shortcuts: F2 barcode, F3 cash, F4 discount, F9 check out
- Admin screens: Employees, Items (with low-stock highlighting), Membership, Sales history
- Automatic stock deduction and a saved record of every sale

## Requirements

- Windows 10 or 11
- .NET SDK matching the version in `DailyGOODS.vbproj`

## Run it

    git clone https://github.com/YOURNAME/DailyGOODS.git
    cd DailyGOODS
    dotnet run

The database is created automatically on first launch at
`%LocalAppData%\DailyGOODS\GrocerySystemInventory.db` with sample data.

## Demo logins

| Role | Employee ID | Password |
|---|---|---|
| Admin | admin | admin123 |
| Cashier | cashier | cashier123 |

These are demo credentials only. Change them before using the app for anything real.

## Customising

- Store name and receipt address: top of `Theme.vb`
- Logo: place a `logo.png` next to the built `DailyGOODS.exe`
- Discount codes: `ApplyDiscount` in `PosForm.vb`