# Commerce API

A basic API with authentication by JWT, product database, custom middlewares and some concepts of asp.net Core.

Warning! It's in development, please don't use in production.

## Database Model:

- Make this query in postgresql in any database:
```SQL
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(150) NOT NULL,
    username VARCHAR(80) NOT NULL,
    password VARCHAR(200) NOT NULL,
    salt VARCHAR(200) NOT NULL,
    latest_jti VARCHAR(32) NOT NULL
)

CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    name VARCHAR(130) NOT NULL,
    description VARCHAR(600) DEFAULT '',
    image_url VARCHAR(200) NOT NULL,
    FOREIGN KEY(user_id) REFERENCES users(id)
)
```

## How to reproduce

### Requirements to reproduce:

- Openssl exe on path;
- PostgreSQL;
- The database model;
- Dotnet CLI;

### Step by step:

- Clone this repositore by:

```shell
$ git clone https://github.com/Dragonabysm/ecommerce-api.git
```

- Generate a RSA private key by:
```shell
$ openssl genrsa -out privatekey.pem 2048
```
AND the public key by:
```shell
$ openssl rsa -in privatekey.pem -out publickey.pem -pubout -outform PEM
```

AND remove the lines of PEM format.

- Change the name of appsettingsSample.json to appsettings.json;

- Insert the RSA public key in the appsettings.json;

- Insert your postgreSQL connection string in appsettings.json;

- Model your database in according with the database model;

- Run:
```shell
$ dotnet user-secrets init
```
AND 
```shell
$ dotnet user-secrets set "Jwt:PrivateKey" "YOUR RSA PRIVATE KEY"
```

- Build and Run:
```shell
$ dotnet run
```
OR
```shell
$ dotnet build
```

- Your browser will open in the swagger documentation. Test the application!