# AbacatePay SDK para CSharp (C#)

Bem-vindo ao SDK oficial do AbacatePay para C#!

Esta biblioteca facilita a integração com a API do AbacatePay, permitindo gerenciar clientes e cobranças via PIX de forma simples e eficiente.

## 📦 Instalação

Instale o pacote via NuGet:

```sh
dotnet add package AbacatePay.SDK
```
Ou via Package Manager:

```sh
Install-Package AbacatePay.SDK
```

## 🚀 Uso

### Configuração do Cliente

```csharp
using AbacatePay.SDK.Core;

var client = new AbacatePayClient("SUA_API_KEY");
```

### 👤 Criando e Consultando Clientes

```csharp
var customerRequest = new CustomerRequest("Nome Cliente", "11999999999", "cliente@email.com", "12345678900");
var createdClient = await client.Customers.CreateClientAsync(customerRequest);

var clients = await client.Customers.GetClientsAsync();
```

### 💳 Criando e Consultando Cobranças

```csharp
List<string> methods = new() { "PIX" };
var productRequest = new ProductRequest("prod_123", "Produto Teste", "Descrição do produto", 1, 1000);
var listProductRequest = new List<ProductRequest> { productRequest };

var billingRequest = new BillingRequest(
    methods,
    listProductRequest,
    new Uri("https://retorno.com"),
    new Uri("https://conclusao.com"),
    "cust_123",
    customerRequest,
    "ONE_TIME"
);

var createdBilling = await client.Billings.CreateBillingAsync(billingRequest);
var billings = await client.Billings.GetBillingsAsync();
```

## 📄 Licença

Este projeto está sob a licença MIT. Consulte o arquivo LICENSE para mais detalhes.

