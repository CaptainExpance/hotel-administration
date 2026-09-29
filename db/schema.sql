   USE HotelDB;
   GO

-- 1-2. Ролевые таблицы
CREATE TABLE Roles (
    RoleId INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(30) NOT NULL UNIQUE
);

CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,   -- только хеш, не пароль
    RoleId INT NOT NULL REFERENCES Roles(RoleId),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

-- 3-4, 7, 9. Справочные таблицы
CREATE TABLE RoomTypes (
    RoomTypeId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Capacity INT NOT NULL CHECK (Capacity > 0)
);

CREATE TABLE RoomStatuses (
    RoomStatusId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(30) NOT NULL UNIQUE
);

CREATE TABLE BookingStatuses (
    BookingStatusId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(30) NOT NULL UNIQUE
);

CREATE TABLE Services (
    ServiceId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Price DECIMAL(10,2) NOT NULL CHECK (Price >= 0)
);

-- 5-6, 8. Основные таблицы
CREATE TABLE Rooms (
    RoomId INT IDENTITY PRIMARY KEY,
    Number NVARCHAR(10) NOT NULL UNIQUE,
    Floor INT NOT NULL,
    RoomTypeId INT NOT NULL REFERENCES RoomTypes(RoomTypeId),
    RoomStatusId INT NOT NULL REFERENCES RoomStatuses(RoomStatusId),
    Price DECIMAL(10,2) NOT NULL CHECK (Price > 0)
);

CREATE TABLE Guests (
    GuestId INT IDENTITY PRIMARY KEY,
    LastName NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    PassportNumber NVARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE Bookings (
    BookingId INT IDENTITY PRIMARY KEY,
    GuestId INT NOT NULL REFERENCES Guests(GuestId),
    RoomId INT NOT NULL REFERENCES Rooms(RoomId),
    UserId INT NOT NULL REFERENCES Users(UserId),   -- кто оформил
    BookingStatusId INT NOT NULL REFERENCES BookingStatuses(BookingStatusId),
    CheckIn DATE NOT NULL,
    CheckOut DATE NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CHECK (CheckOut > CheckIn)
);

-- 10. Связующая таблица (многие ко многим)
CREATE TABLE BookingServices (
    BookingId INT NOT NULL REFERENCES Bookings(BookingId),
    ServiceId INT NOT NULL REFERENCES Services(ServiceId),
    Quantity INT NOT NULL DEFAULT 1 CHECK (Quantity > 0),
    PRIMARY KEY (BookingId, ServiceId)
);

CREATE TABLE Payments (
    PaymentId INT IDENTITY PRIMARY KEY,
    BookingId INT NOT NULL REFERENCES Bookings(BookingId),
    Amount DECIMAL(10,2) NOT NULL CHECK (Amount > 0),
    Method NVARCHAR(30) NOT NULL,
    PaidAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

-- 12. Служебная таблица
CREATE TABLE ActionLog (
    LogId INT IDENTITY PRIMARY KEY,
    UserId INT NULL REFERENCES Users(UserId),
    Action NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

-- Тестовые данные
INSERT INTO Roles (RoleName) VALUES (N'admin'), (N'user');

INSERT INTO Users (Login, PasswordHash, RoleId) VALUES
 (N'admin', CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', 'admin123'), 2), 1),
 (N'user',  CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', 'user123'), 2), 2);

INSERT INTO RoomTypes (Name, Capacity) VALUES (N'Стандарт', 2), (N'Люкс', 3);
INSERT INTO RoomStatuses (Name) VALUES (N'Свободен'), (N'Занят'), (N'Уборка');
INSERT INTO BookingStatuses (Name) VALUES (N'Новая'), (N'Заселён'), (N'Завершена'), (N'Отменена');
INSERT INTO Services (Name, Price) VALUES (N'Завтрак', 500), (N'Трансфер', 1200);

INSERT INTO Rooms (Number, Floor, RoomTypeId, RoomStatusId, Price) VALUES
 (N'101', 1, 1, 1, 3500), (N'102', 1, 1, 2, 3500), (N'201', 2, 2, 1, 7800);
GO