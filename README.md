# meetMi - Micro-Social Platform

<img width="950" height="385" alt="Screenshot 2026-03-06 202414" src="https://github.com/user-attachments/assets/60ecc61f-56cb-4652-929c-0c8d3fad8a76" />

## Project Overview
**meetMi** is a full-stack web application developed in **ASP.NET Core MVC**, designed to facilitate social interaction through personalized profiles, group management, and AI-driven content moderation.

---

## technologies used
* **Backend**: ASP.NET Core MVC (C#)
* **Database & ORM**: SQL Server via Entity Framework Core (Code First)
* **Security & Auth**: ASP.NET Identity (Role-Based Access Control: Visitor, User, Admin)
* **AI Integration**: Google Gemini API (Real-time content filtering)
* **Frontend**: Tailwind CSS, AJAX (for real-time interactions), Razor Views

---

## Technical Features

### 1. Social Graph & Visibility Logic
* **Follow System**: Implemented a unidirectional "Follow" mechanism.
* **Privacy Control**: Integrated status-based visibility (Public vs. Private). Private profiles require manual approval of "Pending" requests to unlock full content access.
* **Global Search**: Optimized search functionality allowing partial name matches for both registered and anonymous users.

### 2. AI Content Guard (Safety Engine)
* **Automated Filtering**: The system intercepts all posts and comments before database persistence.
* **Gemini API Integration**: Sends user input to the AI model to detect hate speech, insults, or discriminatory language.
* **Prevention Logic**: Blocks toxic content at the controller level and returns user-friendly validation errors without refreshing the page.

### 3. Media & Interaction Management
* **Multi-format Posts**: Support for Text, Image, and Video content delivery.
* **Group Dynamics**: Creator-moderated groups with "Join Request" workflows and administrative control over messaging.
* **Dynamic Feed**: A personalized content stream that aggregates posts from followed users, sorted chronologically.

### 4. Administrative Dashboard
* **Full CRUD Control**: Specialized middleware allowing Admins to moderate or delete any platform content (comments, groups, users) to maintain community standards.
* **Database Seeding**: Automated seeding for realistic initial data (Users, Roles, Groups, and Posts).

---

## Architecture Details
* **MVC Pattern**: Strict separation of concerns between Models (data structures), Views (Tailwind-styled UI), and Controllers (business logic).
* **Persistent Storage**: Complex relationships handled via EF Core (One-to-Many for comments, Many-to-Many for group memberships and follows).
* **Real-time Updates**: Used AJAX to ensure content updates appear without full page reloads.
