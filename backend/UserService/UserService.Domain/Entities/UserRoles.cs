namespace UserService.Domain.Entities;

public enum UserRole
{
	Regular = 0,
	Admin = 1,
	SuperAdmin = 2,
	// Non-human principal — used by Shelter.Ingestion automation to write records.
	// Allowed: POST/PUT/PATCH on locations. NOT allowed: DELETE (only humans can delete).
	ServiceAccount = 3
}
