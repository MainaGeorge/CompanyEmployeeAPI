# CompanyEmployeeAPI
This is an api project for managing employees and their companies done with aspnet core 5. The focus is on creating restful api and following the rules for creating parent and child
resources. The app is an imaginary collection of companies that have employees. An employee can only exist in a company, and a company can have more than one employees but 
an employee belongs to only one company.

the api exposes endpoints to create both the parent(company) and child(employee) resources using the post method, updating them using the put and patch http methods
and deleting the resources using the delete method.

the api also allows specification of the particular properties of the resource that the client requires thanks to data shaping.

the api also allows creation of a collection of resources

the api comes with data validation already 
