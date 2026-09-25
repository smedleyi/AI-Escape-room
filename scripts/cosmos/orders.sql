-- One JSON document per order, with line items embedded at their purchase-time price.
SET NOCOUNT ON;
SELECT (
    SELECT
        CAST(o.Id AS NVARCHAR(20)) AS id,
        o.Email AS email,
        'order' AS type,
        o.CustomerName AS customerName,
        o.TotalAmount AS totalAmount,
        o.OrderDate AS orderDate,
        o.ShippingRegion AS shippingRegion,
        o.Status AS status,
        (
            SELECT
                oi.ProductId AS productId,
                p.Name AS name,
                oi.Quantity AS quantity,
                oi.UnitPrice AS unitPrice
            FROM OrderItems oi
            JOIN Products p ON p.Id = oi.ProductId
            WHERE oi.OrderId = o.Id
            FOR JSON PATH
        ) AS items
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
)
FROM Orders o
ORDER BY o.Id;
