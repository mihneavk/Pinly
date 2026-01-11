# 📌 Pinly - Platformă Social Media

Pinly este o aplicație web socială construită cu **ASP.NET Core MVC**, care combină funcționalități de partajare media (stil Pinterest) cu un sistem avansat de chat și moderare automată bazată pe Inteligență Artificială.

## 🚀 Funcționalități

### 1. Management Pin-uri (Media)
* **Upload:** Suport pentru imagini și fișiere video.
* **Capacitate:** Limită de upload extinsă până la **500MB** per fișier.
* **Interacțiuni:**
    * Like-uri la postări.
    * Comentarii la postări.
    * Like-uri la comentarii.
* **Notificări:** Sistem de notificări interne pentru aprecieri și comentarii.

### 2. Chat & Comunitate
* **Mesaje Private (DM):** Conversații 1-la-1 cu funcție de **Block/Unblock**.
* **Grupuri Private:** Acces pe bază de invitație (Admin/Moderator).
* **Grupuri Publice:**
    * Listă publică de grupuri cu descrieri.
    * Sistem **Join Request** (membrii cer acces, moderatorii aprobă/resping).
    * Adăugarea manuală de membri este dezactivată pentru grupurile publice.
* **Roluri & Permisiuni:**
    * **Admin (Creator):** Control total, promovare moderatori.
    * **Moderator:** Poate accepta cereri, da kick la membri (dar nu la alți moderatori/admin).
    * **Membru:** Poate trimite mesaje și părăsi grupul.

### 3. 🤖 AI Companion (Moderare Automată)
* Integrare cu **Hugging Face Inference API** (model `unitary/toxic-bert`).
* **Protecție activă:** Sistemul scanează și blochează automat:
    * Mesaje din chat cu conținut toxic/insulte.
    * Titluri și descrieri de Pin-uri neadecvate.
    * Comentarii cu limbaj ofensator.
    * Nume și descrieri de grupuri la creare.

## 🛠️ Tehnologii Utilizate

* **Backend:** C# .NET 8 (ASP.NET Core MVC)
* **Database:** SQL Server (Entity Framework Core)
* **Auth:** ASP.NET Core Identity
* **AI:** Hugging Face API (`HttpClient`)
* **Frontend:** Razor Views, Bootstrap 5, JavaScript

## ⚙️ Configurare și Rulare

### 1. Pre-condiții
* .NET SDK instalat.
* SQL Server (LocalDB sau instanță completă).
* Un cont și cheie API (gratuită) de la [Hugging Face](https://huggingface.co/).

### 2. Clonare și Configurare
Clonează repository-ul și deschide soluția în Visual Studio.

### 3. Setare Bază de Date
Verifică `appsettings.json` pentru connection string. Apoi rulează în Package Manager Console:

```bash
Update-Database
