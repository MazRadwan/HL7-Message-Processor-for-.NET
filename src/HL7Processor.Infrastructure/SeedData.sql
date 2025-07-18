-- Seed data for HL7 Processor Dashboard
USE HL7ProcessorDb;

-- Add sample messages for the dashboard
INSERT INTO Messages (Id, MessageType, Version, Timestamp, ProcessingStatus, PatientId) VALUES
(NEWID(), 'ADT^A01', '2.4', DATEADD(HOUR, -2, GETDATE()), 'Processed', 'P001234'),
(NEWID(), 'ADT^A08', '2.4', DATEADD(HOUR, -1, GETDATE()), 'Processed', 'P001235'),
(NEWID(), 'ORU^R01', '2.4', DATEADD(MINUTE, -30, GETDATE()), 'Processed', 'P001236'),
(NEWID(), 'ADT^A01', '2.4', DATEADD(MINUTE, -25, GETDATE()), 'Pending', 'P001237'),
(NEWID(), 'ORU^R01', '2.4', DATEADD(MINUTE, -20, GETDATE()), 'Processing', 'P001238'),
(NEWID(), 'ADT^A08', '2.4', DATEADD(MINUTE, -15, GETDATE()), 'Error', 'P001239'),
(NEWID(), 'ORU^R01', '2.4', DATEADD(MINUTE, -10, GETDATE()), 'Processed', 'P001240'),
(NEWID(), 'ADT^A01', '2.4', DATEADD(MINUTE, -5, GETDATE()), 'Processed', 'P001241'),
(NEWID(), 'ORU^R01', '2.4', DATEADD(MINUTE, -2, GETDATE()), 'Pending', 'P001242'),
(NEWID(), 'ADT^A08', '2.4', DATEADD(MINUTE, -1, GETDATE()), 'Processed', 'P001243');

-- Add some historical data for the last few days
DECLARE @i INT = 1;
WHILE @i <= 50
BEGIN
    INSERT INTO Messages (Id, MessageType, Version, Timestamp, ProcessingStatus, PatientId) VALUES
    (NEWID(), 
     CASE (@i % 3) 
         WHEN 0 THEN 'ADT^A01'
         WHEN 1 THEN 'ORU^R01' 
         ELSE 'ADT^A08'
     END,
     '2.4',
     DATEADD(HOUR, -(@i % 72), GETDATE()),
     CASE (@i % 10)
         WHEN 0 THEN 'Error'
         WHEN 1 THEN 'Pending'
         WHEN 2 THEN 'Processing'
         ELSE 'Processed'
     END,
     'P' + RIGHT('00000' + CAST((1000 + @i) AS VARCHAR), 6)
    );
    SET @i = @i + 1;
END;

-- Verify the data
SELECT 
    ProcessingStatus,
    COUNT(*) as Count
FROM Messages 
GROUP BY ProcessingStatus;

SELECT 
    CAST(Timestamp AS DATE) as Date,
    COUNT(*) as MessageCount
FROM Messages 
GROUP BY CAST(Timestamp AS DATE)
ORDER BY Date DESC;