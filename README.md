<p>
  <h3 align="center">Asp .Net Membership Reset</h3>
  <p align="center">Simple Asp .Net Membership reset tools </p>
</p>



## The Project
This project / app created to make create dummy user or reset existing user is easy when you don't have login access to Asp .Net Membership management page.



## Getting Started
As for now there's no binary provided, you need to manually build the project yourself. 
It should be easy enough.



### Prerequisites
The project developed under [.Net Framework 4.5](https://dotnet.microsoft.com/download/dotnet-framework), which come pre-installed on Visual Studio 2012 and also should be existed inside latest Windows 10 version.



### Installation
There's no installation needed, as this app is a simple console app. 



## Usage
There are several mode / menu that you can use. And you'll be asked some parameter needed for each mode.  



### 1. Reset
This mode is intended to reset the specified user's password. Parameters below are expected.  

| Parameter         | Value                                                  |
| ----------------- | ------------------------------------------------------ |
| Mode              | R _{For Reset obviously}_                              |
| Connection String | _{You can get this from your app.config / web.config}_ |
| App Name          | _{You can get this from your app.config / web.config}_ |
| Hash Algo         | _{You can get this from your app.config / web.config}_ |
| Username          | _{Username you want to reset}_                         |
| Password          | _{New password}_                                       |



### 2. Create
This mode is intended to create new user as specified. Parameters below are expected.  

| Parameter         | Value                                                                                  |
| ----------------- | -------------------------------------------------------------------------------------- |
| Mode              | C                                                                                      |
| Connection String | _{You can get this from your app.config / web.config}_                                 |
| App Name          | _{You can get this from your app.config / web.config}_                                 |
| Hash Algo         | _{You can get this from your app.config / web.config}_                                 |
| Username          | _{Username you want to create}_                                                        |
| Password          | _{User password}_                                                                      |
| Email             | _{User email}_                                                                         |
| Role              | _{Select 1 from above list (it'll displayed to you, and currently can only choose 1)}_ |



### 3. Delete
This mode is intended to delete specified user. Parameters below are expected.  

| Parameter         | Value                                                  |
| ----------------- | ------------------------------------------------------ |
| Mode              | D                                                      |
| Connection String | _{You can get this from your app.config / web.config}_ |
| App Name          | _{You can get this from your app.config / web.config}_ |
| Hash Algo         | _{You can get this from your app.config / web.config}_ |
| Username          | _{Username you want to delete}_                        |



### 4. View
This mode is intended to reset the specified user's password. Parameters below are expected.  

| Parameter         | Value                                                  |
| ----------------- | ------------------------------------------------------ |
| Mode              | V                                                      |
| Connection String | _{You can get this from your app.config / web.config}_ |
| App Name          | _{You can get this from your app.config / web.config}_ |
| Hash Algo         | _{You can get this from your app.config / web.config}_ |
| Username          | _{Username you want to view}_                          |

These info will be displayed to you:
* Application name
* Application description
* Username
* Last activity date
* Email
* Approved status
* Locked out status
* Last login date
* Last password changed date
* Last locked out date
* Failed login attempt count
* Failed password answer attempt count
* Role name
* Role description
* Profile names
* Profile values



### 5. List
This mode is intended to list all users in the database. Parameters below are expected.  

| Parameter         | Value                                                  |
| ----------------- | ------------------------------------------------------ |
| Mode              | L                                                      |
| Connection String | _{You can get this from your app.config / web.config}_ |

Application name and username will be displayed to you



## License
Distributed under the UNLICENSE License. See `LICENSE` for more information.
