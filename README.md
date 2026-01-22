# 🛒 MicroShop Backend

![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)
![Apache Kafka](https://img.shields.io/badge/Apache%20Kafka-000?style=for-the-badge&logo=apachekafka)
![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white)
![ElasticSearch](https://img.shields.io/badge/-ElasticSearch-005571?style=for-the-badge&logo=elasticsearch)
![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white)

A robust, event-driven e-commerce backend built with **ASP.NET Core** microservices. This project demonstrates a scalable architecture using **Kafka** for asynchronous messaging, **Redis** for caching, **Elasticsearch** for high-performance search, and an **API Gateway** to manage traffic.

---

## 🏗 Architecture

The system is composed of several independent microservices orchestrating e-commerce flows:

| Service | Description | Port |
| :--- | :--- | :--- |
| **API Gateway** | Entry point for all client requests (Ocelot). | `8080` |
| **Auth API** | Handles user authentication and JWT issuance. | `5001` |
| **Product API** | Manages catalog & inventory; integrates with Elasticsearch. | `5002` |
| **Payment API** | Mock payment processing service. | `5003` |
| **Cart API** | High-performance shopping cart management using **Redis**. | `5004` |
| **Producer API** | Receives orders and publishes events to **Kafka**. | `5000` |
| **Consumer Worker** | Background worker processing Kafka order events. | N/A |

### ⚡ Key Features
* **Microservices Architecture**: Decoupled services for scalability.
* **Event-Driven**: Uses **Apache Kafka** for asynchronous order processing.
* **CQRS Pattern**: Separation of read and write operations (via Producer/Consumer).
* **High Performance**: **Redis** caching for shopping carts and **Dapper** for fast SQL queries.
* **Advanced Search**: Full-text search capabilities powered by **Elasticsearch**.
* **Containerized**: Fully Dockerized environment for easy setup.

---

## 🚀 Getting Started

### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop)
* .NET 8 SDK (for local development)

### Quick Start (Docker)
The easiest way to run the entire suite is via Docker Compose.

1.  **Clone the repository**
    ```bash
    git clone [https://github.com/rahul-raj1709/microshop-backend.git](https://github.com/rahul-raj1709/microshop-backend.git)
    cd microshop-backend
    ```

2.  **Start the services**
    ```bash
    docker-compose up --build -d
    ```
    *This will spin up Postgres, Kafka, Zookeeper, Redis, Elasticsearch, and all microservices.*

3.  **Verify Status**
    Ensure all containers are healthy:
    ```bash
    docker ps
    ```

---

## 🛠 Tech Stack

* **Framework**: ASP.NET Core 8
* **Database**: PostgreSQL
* **ORM**: Dapper (for performance) & EF Core
* **Messaging**: Apache Kafka (Confluent)
* **Caching**: Redis
* **Search**: Elasticsearch
* **Gateway**: Ocelot
* **Containerization**: Docker & Docker Compose

## 📂 Project Structure

```text
microshop-backend/
├── ApiGateway/       # Ocelot Gateway configuration
├── AuthAPI/          # Identity and User management
├── CartAPI/          # Redis-backed shopping cart
├── ConsumerWorker/   # Background Kafka consumer
├── PaymentAPI/       # Payment processing logic
├── ProducerAPI/      # Order creation & event publishing
├── ProductAPI/       # Catalog & Search logic
└── docker-compose.yml
