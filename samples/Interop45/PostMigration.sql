update dbo.AspNetUsers set NormalizedUserName = UPPER(UserName);
Go
update dbo.AspNetUsers set NormalizedEmail = UPPER(Email);
Go
update dbo.AspNetRoles set NormalizedName = UPPER(Name);
Go
