USE [Hub]
GO
/****** Object:  Table [dbo].[EventSource]    Script Date: 11/5/2018 6:18:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EventSource](
	[Source_ID] [int] IDENTITY(1,1) NOT NULL,
	[Source_Name] [varchar](50) NULL
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[EventSource] ON 

INSERT [dbo].[EventSource] ([Source_ID], [Source_Name]) VALUES (1, N'Voyager')
INSERT [dbo].[EventSource] ([Source_ID], [Source_Name]) VALUES (2, N'Salesforce')
INSERT [dbo].[EventSource] ([Source_ID], [Source_Name]) VALUES (3, N'Renowalk')
INSERT [dbo].[EventSource] ([Source_ID], [Source_Name]) VALUES (4, N'Fotonotes')
INSERT [dbo].[EventSource] ([Source_ID], [Source_Name]) VALUES (5, N'RentCafe')
INSERT [dbo].[EventSource] ([Source_ID], [Source_Name]) VALUES (6, N'Fischer')
SET IDENTITY_INSERT [dbo].[EventSource] OFF
