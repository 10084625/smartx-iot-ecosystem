# SmartX IoT Ecosystem – Sensor Ingestion and Gateway Console

IoT back end and desktop console

## About

Platform for registering IoT sensors (environmental, power consumption and actuators) by MAC address and zone, taking in their readings, and attaching hardware logs or photos.

Checks multi-level site layouts (facility, zone, sub-zone), totals meter readings, and gives each sensor a live health score that flags unusual readings.

## Built with

- C#
- ASP.NET Core minimal API
- EF Core
- PostgreSQL
- WPF
- Docker

## Getting started

This repository has two parts:

- `SmartX.Api/` – the current ASP.NET Core API using PostgreSQL. Run it with `dotnet run`, or build the Docker image with the included `Dockerfile`.
- `SmartX_IoT_Ecosystem/` – the original solution, including the WPF desktop gateway console (`SmartX.Client`). Open `SmartX_IoT.slnx` in Visual Studio.

## Configuration

Set the `ConnectionStrings:SmartX` value (or the `ConnectionStrings__SmartX` environment variable) to your PostgreSQL database.

## Developer

**Caleb Naidoo** · [LinkedIn](https://www.linkedin.com/in/caleb-naidoo-3a0312254) · calebnaidoo109@gmail.com
