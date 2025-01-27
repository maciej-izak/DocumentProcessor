# DocumentProcessor API

[![GitHub Actions Status](https://github.com/maciej-izak/DocumentProcessor/actions/workflows/ci.yml/badge.svg)](https://github.com/maciej-izak/DocumentProcessor/actions)

## Overview

**DocumentProcessor API** is a robust .NET 9.0 web API designed to process and manage documents efficiently. It supports file uploads, parses document data, performs validations, and provides aggregated statistics. The API is secured with Basic Authentication and includes comprehensive unit tests to ensure reliability.

## Features

- **File Processing:** Upload and process documents with multiple positions.
- **Basic Authentication:** Secure API endpoints using Basic Authentication.
- **Validation:** Ensure data integrity by validating document and position details.
- **Health Checks:** Monitor the health of the API with built-in health checks.
- **Unit Testing:** Comprehensive tests using xUnit and Moq.
- **Continuous Integration:** Automated workflows with GitHub Actions.

## Technologies Used

- **.NET 9.0**
- **ASP.NET Core**
- **xUnit** for unit testing
- **Moq** for mocking dependencies
- **GitHub Actions** for CI/CD

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/downloads)

### Installation

1. **Clone the Repository**

   ```bash
   git clone https://github.com/maciej-izak/DocumentProcessor.git
   cd DocumentProcessor
   ```

2. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

3. **Build the Project**

   ```bash
   dotnet build
   ```

### Configuration

The API uses Basic Authentication with the following credentials:

- **Username:** `vs`
- **Password:** `rekrutacja`

Ensure that any client interacting with the API includes these credentials in the `Authorization` header.

### Running the API

To run the API locally:

```bash
dotnet run --project DocumentProcessorApi
```

The API will be available at `https://localhost:7275` or `http://localhost:5150`.

### API Endpoints

- **Process File**

  ```
  POST /api/test/{x}
  ```

  - **Parameters:**
    - `x` (int): A parameter used in processing logic.
  - **Body:**
    - Form-data with a file upload (`IFormFile`).

- **Health Check**

  ```
  GET /health
  ```

  - Returns the health status of the API.

### Running Tests

The project includes unit tests to ensure functionality and reliability.

1. **Navigate to the Tests Directory**

   ```bash
   cd DocumentProcessorApi.Tests
   ```

2. **Run Tests**

   ```bash
   dotnet test
   ```

   This command will execute all tests and provide a summary of the results.

### Continuous Integration with GitHub Actions

The project is configured with GitHub Actions to automate testing and deployment workflows. Every push to the repository triggers the CI pipeline, ensuring that all tests pass before changes are merged.

You can view the workflow runs and their statuses here:

[![GitHub Actions Status](https://github.com/maciej-izak/DocumentProcessor/actions/workflows/ci.yml/badge.svg)](https://github.com/maciej-izak/DocumentProcessor/actions)

For more details, visit the [GitHub Actions](https://github.com/maciej-izak/DocumentProcessor/actions) page of this repository.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any enhancements or bug fixes.

## License

This project is licensed under the [MIT License](LICENSE).

---

**Happy Coding!**
