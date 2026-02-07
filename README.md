# Task Station

Sistema de gerenciamento de tarefas com controle de SLA (Service Level Agreement) e suporte a anexos de arquivos.

## 🎥 Demonstração

**[▶️ Assista à demonstração da aplicação em funcionamento](https://drive.google.com/file/d/1laViePmCm6vn55pKBwga7f2mbpIalJFF/view?usp=sharing)**

---

## Sumário

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Design Patterns](#design-patterns)
- [Bibliotecas e Pacotes](#bibliotecas-e-pacotes)
- [Maiores Desafios](#maiores-desafios)
- [Como Executar](#como-executar)
- [Endpoints da API](#endpoints-da-api)
- [Testes](#testes)

---

## Visão Geral

Task Station é uma aplicação full-stack para gerenciamento de tarefas com monitoramento de SLA. O sistema permite:

- ✅ Criação de tarefas com título e prazo (SLA em horas)
- 📎 Upload de anexos (armazenados em S3 ou MongoDB)
- ⏰ Cálculo automático de data de vencimento
- 🎯 Filtro por status (PENDING, DONE, OVERDUE)
- ✏️ Atualização de tarefas e mudança de status
- 📥 Download de arquivos anexados

---

## Arquitetura

O projeto segue os princípios da **Clean Architecture**, garantindo separação de responsabilidades, testabilidade e manutenibilidade.

### Backend (.NET 8)

```
backend/
├── src/
│   ├── TaskStation.Domain/          # Camada de Domínio (Entidades, Regras de Negócio)
│   ├── TaskStation.Application/     # Camada de Aplicação (Casos de Uso, DTOs, Validadores)
│   ├── TaskStation.Infrastructure/  # Camada de Infraestrutura (Persistência, Storage, Mensageria)
│   └── TaskStation.API/             # Camada de Apresentação (Controllers, Middleware)
└── tests/
    ├── TaskStation.Tests/           # Testes Unitários
    └── TaskStation.IntegrationTests/ # Testes de Integração
```

**Camadas da Clean Architecture:**

1. **Domain** - Coração do sistema, contém:
   - `TaskItem`: Entidade rica com invariantes e comportamentos
   - `ITaskRepository`: Contrato de persistência
   - Exceções de domínio (`DomainException`, `EntityNotFoundException`, `TaskValidationException`)

2. **Application** - Orquestração de casos de uso:
   - `TaskAppService`: Implementação dos casos de uso
   - DTOs para comunicação com a API
   - Validadores FluentValidation
   - Interfaces de serviços (`ITaskAppService`, `IFileStorageService`)

3. **Infrastructure** - Detalhes de implementação:
   - `TaskRepository`: Implementação com MongoDB
   - `S3FileStorageService`: Upload para S3/LocalStack
   - `MongoDbContext`: Configuração e mapeamento do MongoDB

4. **API** - Camada Web:
   - Controllers RESTful
   - Middleware de tratamento de exceções
   - Configuração de DI, CORS, Swagger

### Frontend (React + Vite)

```
frontend/
├── src/
│   ├── components/          # Componentes React (Modals, Tabelas, Cards)
│   ├── lib/                 # Utilitários (API client, helpers)
│   ├── hooks/               # Custom hooks (useQuery, useMutation)
│   └── types/               # TypeScript types
└── public/                  # Arquivos estáticos
```

### Infraestrutura (Docker)

- **MongoDB** - Banco de dados NoSQL para persistência de tarefas
- **LocalStack** - Emulador local de AWS S3 para armazenamento de arquivos
- **API (.NET)** - Backend containerizado
- **Frontend (React)** - Aplicação web containerizada
- **Nginx** - Reverse proxy (produção)

---

## Design Patterns



### 1. **Repository Pattern**

**Por quê?** Abstrai a lógica de persistência, permitindo trocar MongoDB por outro banco sem afetar a aplicação.

```csharp
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(string id, CancellationToken ct);
    Task InsertAsync(TaskItem task, CancellationToken ct);
}
```




### 2. **Middleware Pattern**

**Por quê?** Tratamento centralizado de exceções e concerns transversais (logging, CORS, autenticação).

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }
}
```

### 3. **Strategy Pattern (File Storage)**

**Por quê?** Permite alternar entre S3 e MongoDB para armazenamento de arquivos via configuração, sem mudança de código.

```csharp
if (_fileStorageSettings.IsS3Enabled)
    task.SetFileUrl(await _fileStorage.UploadAsync(...));
else
    task.SetFileData(fileBytes, fileName, contentType);
```

---

## Bibliotecas e Pacotes

### Backend (.NET 8)

| Pacote | Versão | Motivação |
|--------|--------|-----------|
| **FluentValidation** | 11.11.0 | Validação declarativa e type-safe. Separa regras de validação da lógica de negócio, tornando o código mais limpo e testável. |
| **MongoDB.Driver** | 3.1.0 | NoSQL permite flexibilidade de schema, ideal para evolução rápida do modelo. Suporte nativo a documentos complexos (arrays, nested objects). |
| **AWSSDK.S3** | 3.7.405.5 | Upload escalável de arquivos. Suporta LocalStack para desenvolvimento local e AWS S3 em produção sem mudanças de código. |
| **Swashbuckle.AspNetCore** | 6.9.0 | Documentação automática da API via Swagger/OpenAPI. Facilita integração frontend e testes. |
| **MassTransit.RabbitMQ** | 8.3.6 | Preparação para processamento assíncrono futuro (notificações de SLA vencido, eventos de domínio). |
| **Microsoft.Extensions.Options** | 8.0.2 | Padrão strongly-typed para configurações, com validação e reload automático. |

### Frontend (React)

| Pacote | Versão | Motivação |
|--------|--------|-----------|
| **Vite** | 5.4.19 | Build tool extremamente rápido (HMR instantâneo), bundle size otimizado. Substitui Create React App com muito melhor DX. |
| **@tanstack/react-query** | 5.83.0 | Gerenciamento de estado server-side com cache inteligente, retry automático e sincronização em background. Reduz boilerplate de loading/error states. |
| **react-hook-form** | 7.61.1 | Performance otimizada (uncontrolled components), validação integrada, excelente DX. |
| **zod** | 3.25.76 | Validação type-safe no frontend, schema reutilizável. Garante consistência entre runtime e TypeScript types. |
| **@radix-ui/*** | - | Componentes primitivos acessíveis (WAI-ARIA compliant) e unstyled. Base sólida para design system customizado. |
| **tailwindcss** | 3.4.17 | Utility-first CSS. Desenvolvimento rápido, bundle size otimizado (PurgeCSS), design system consistente via config. |
| **axios** | 1.7.9 | Interceptors para tratamento global de erros, suporte a multipart/form-data, cancelamento de requests. |
| **date-fns** | 3.6.0 | Leve e modular (tree-shakeable), excelente suporte a internacionalização e fusos horários. |
| **sonner** | 1.7.4 | Toast notifications modernas e acessíveis, com animações suaves e stacking automático. |
| **lucide-react** | 0.462.0 | Ícones SVG modernos, tree-shakeable, consistentes com design system. |

---

## Maiores Desafios

### 1. **Implementação de Armazenamento Dual (S3 vs MongoDB)**

**Problema:** Ocorreu problemas criação do s3 em conta pessoal, (conta da aws pessoal não estava validando o login)

- **S3/LocalStack** (produção): Escalável, mas requer configuração adicional
- **MongoDB** (desenvolvimento): Simples, mas limitado a 16MB por documento

**Solução:** Strategy pattern com flag de configuração (`S3.Enabled`). A entidade de domínio (`TaskItem`) suporta ambos os cenários:

```csharp
// S3 mode
task.SetFileUrl("https://s3.amazonaws.com/bucket/file.pdf");

// MongoDB mode
task.SetFileData(fileBytes, "file.pdf", "application/pdf");
```

**Complexidade adicional:**
- MongoDB requer armazenamento de `FileData`, `FileName` e `FileContentType`
- S3 armazena apenas `FileUrl`
- Endpoint de download (`GET /api/tasks/{id}/file`) precisa servir arquivos do MongoDB quando S3 está desabilitado
- Mapper precisa gerar `FileUrl` correta baseado no modo de armazenamento

### 2. **Gerenciamento de SLA e Detecção de Vencimento**

**Problema:** Calcular automaticamente tarefas vencidas (OVERDUE) considerando:
- Data de criação + SLA em horas = Data de vencimento
- Tarefas concluídas (DONE) não podem ficar OVERDUE
- Filtro deve retornar apenas tarefas realmente vencidas no momento da consulta

**Solução:**
- `DueDate` calculado na criação: `CreatedAt.AddHours(SlaHours)`
- Método de domínio `IsSlaExpired()` para verificação
- Query MongoDB otimizada para OVERDUE:

```csharp
public async Task<IReadOnlyList<TaskItem>> GetOverdueTasksAsync(CancellationToken ct)
{
    return await _collection
        .Find(t => t.Status != TaskItemStatus.Done && t.DueDate < DateTime.UtcNow)
        .ToListAsync(ct);
}
```

### 3. **Upload de Arquivos com Multipart/Form-Data**

**Problema:** ASP.NET Core não suporta `IFormFile` em DTOs simples com `[FromBody]`.

**Solução:**
- Usar `[FromForm]` nos controllers
- Validação customizada de extensões permitidas (`.pdf`, `.png`, `.jpg`, `.docx`, etc.)
- Limite de tamanho (10MB) validado via FluentValidation:

```csharp
RuleFor(x => x.File!.Length)
    .LessThanOrEqualTo(10 * 1024 * 1024)
    .WithMessage("File size must not exceed 10 MB.");
```

### 4. **Mapeamento MongoDB com BsonClassMap**

**Problema:** MongoDB Driver 3.x exige configuração explícita para mapear `string Id` corretamente (auto-geração de ObjectId).

**Solução:**
```csharp
BsonClassMap.RegisterClassMap<TaskItem>(cm =>
{
    cm.AutoMap();
    cm.MapIdMember(c => c.Id)
      .SetIdGenerator(StringObjectIdGenerator.Instance)
      .SetSerializer(new StringSerializer(BsonType.ObjectId));
});
```

### 5. **Tratamento Centralizado de Exceções**

**Problema:** Exceções de domínio, validação e infraestrutura precisam retornar códigos HTTP corretos.

**Solução:** `GlobalExceptionMiddleware` com pattern matching:
- `TaskValidationException` → 400 Bad Request
- `EntityNotFoundException` → 404 Not Found
- `DomainException` → 422 Unprocessable Entity
- Outros → 500 Internal Server Error

### 6. **Configuração de CORS entre Containers Docker**

**Problema:** Frontend (porta 5173) precisa acessar API (porta 5000) em ambiente containerizado.

**Solução:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

### 7. **Testes de Integração com MongoDB**

**Problema:** Testes de integração precisam de banco de dados real, mas não devem poluir dados entre testes.

**Solução:**
- `WebApplicationFactory` para testar API completa
- Banco de dados de teste (`TaskStationDb_Test`)
- Cleanup automático após cada teste:

```csharp
[TearDown]
public async Task TearDown()
{
    await _mongoClient.DropDatabaseAsync("TaskStationDb_Test");
}
```

---

## Como Executar

### Pré-requisitos

- **.NET SDK 8.0+**
- **Node.js 20+**
- **Docker & Docker Compose**

### Opção 1: Docker Compose (Recomendado)

```bash
# Clonar repositório
git clone <repository-url>
cd Task-Station

# Subir todos os serviços
docker-compose up --build

# Acessar aplicação
# Frontend: http://localhost:5173
# API: http://localhost:5000/swagger
# MongoDB: localhost:27017
```

### Opção 2: Desenvolvimento Local

**Backend:**
```bash
cd backend/src/TaskStation.API
dotnet restore
dotnet run

# API disponível em http://localhost:5000
# Swagger em http://localhost:5000/swagger
```

**Frontend:**
```bash
cd frontend
npm install
npm run dev

# App disponível em http://localhost:5173
```

**MongoDB:**
```bash
docker run -d -p 27017:27017 --name mongodb mongo:7
```

### Configuração do Armazenamento de Arquivos

No arquivo `backend/src/TaskStation.API/appsettings.json`:

```json
{
  "S3": {
    "Enabled": false,  // true = S3/LocalStack, false = MongoDB
    "ServiceUrl": "http://localhost:4566",
    "BucketName": "task-station-files",
    "Region": "us-east-1"
  }
}
```

---

## Endpoints da API

### Tasks

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/tasks` | Cria nova tarefa (multipart/form-data) |
| `GET` | `/api/tasks` | Lista todas as tarefas (com filtro opcional `?status=PENDING`) |
| `GET` | `/api/tasks/{id}` | Busca tarefa por ID |
| `PUT` | `/api/tasks/{id}` | Atualiza tarefa completa |
| `PATCH` | `/api/tasks/{id}` | Atualiza apenas o status |
| `GET` | `/api/tasks/{id}/file` | Download do arquivo anexado |

### Exemplos

**Criar Tarefa:**
```bash
curl -X POST http://localhost:5000/api/tasks \
  -F "title=Implementar feature X" \
  -F "slaHours=24" \
  -F "file=@documento.pdf"
```

**Listar Tarefas Vencidas:**
```bash
curl http://localhost:5000/api/tasks?status=OVERDUE
```

**Marcar como Concluída:**
```bash
curl -X PATCH http://localhost:5000/api/tasks/{id} \
  -H "Content-Type: application/json" \
  -d '{"status": "DONE"}'
```

---

## Testes

### Testes Unitários

```bash
cd backend/tests/TaskStation.Tests
dotnet test
```

**Cobertura:**
- ✅ Validadores FluentValidation
- ✅ Entidade de domínio (`TaskItem`)
- ✅ Exceções customizadas
- ✅ Mappers

### Testes de Integração

```bash
cd backend/tests/TaskStation.IntegrationTests
dotnet test
```

**Cobertura:**
- ✅ Controllers (end-to-end)
- ✅ Persistência MongoDB
- ✅ Upload de arquivos
- ✅ Filtros de status
- ✅ Validação de regras de negócio

**Detalhes:** Ver [backend/tests/TaskStation.IntegrationTests/INTEGRATION-TESTS-README.md](backend/tests/TaskStation.IntegrationTests/INTEGRATION-TESTS-README.md)

---

## Tecnologias Utilizadas

### Backend
- .NET 8
- ASP.NET Core Web API
- MongoDB Driver
- FluentValidation
- AWS SDK S3
- MassTransit (RabbitMQ)
- Swagger/OpenAPI

### Frontend
- React 18
- TypeScript
- Vite
- TanStack Query
- React Hook Form
- Zod
- Radix UI
- Tailwind CSS
- Axios

### Infraestrutura
- Docker & Docker Compose
- MongoDB 7
- LocalStack (S3)
- Nginx

---

## Estrutura de Pastas Completa

```
Task-Station/
├── backend/
│   ├── src/
│   │   ├── TaskStation.Domain/
│   │   │   ├── Entities/
│   │   │   ├── Enums/
│   │   │   ├── Exceptions/
│   │   │   └── Repositories/
│   │   ├── TaskStation.Application/
│   │   │   ├── DTOs/
│   │   │   ├── Interfaces/
│   │   │   ├── Mappers/
│   │   │   ├── Services/
│   │   │   └── Validators/
│   │   ├── TaskStation.Infrastructure/
│   │   │   ├── Persistence/
│   │   │   └── Storage/
│   │   └── TaskStation.API/
│   │       ├── Controllers/
│   │       ├── Middleware/
│   │       └── Program.cs
│   └── tests/
│       ├── TaskStation.Tests/
│       └── TaskStation.IntegrationTests/
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   ├── lib/
│   │   ├── hooks/
│   │   └── types/
│   └── public/
├── docker-compose.yml
└── README.md
```

---

## Licença

Este projeto foi desenvolvido como teste técnico.

---

## Autor

Desenvolvido com .NET, React e boas práticas de Clean Architecture.
