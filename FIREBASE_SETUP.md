# Firebase Setup

This app can store its shop data in Cloud Firestore instead of the local JSON file.

## What You Need

- Firebase project id.
- A Firebase service account JSON file. This file includes `client_email`, `private_key`, and other credential fields.
- Firestore enabled in the Firebase project.

Do not commit or share the service account JSON. The repo already ignores common Firebase credential filenames and `App_Data/*.json`.

## Local Setup

1. In Firebase Console, open Project settings > Service accounts.
2. Generate a new private key and download the JSON file.
3. Put it here:

```text
src/MonkFruitStore.Web/App_Data/firebase-service-account.json
```

4. Copy this file:

```text
src/MonkFruitStore.Web/appsettings.Local.example.json
```

to:

```text
src/MonkFruitStore.Web/appsettings.Local.json
```

5. Edit `appsettings.Local.json` and set your Firebase project id.

When Firestore is empty, the app imports the current local `App_Data/store.json` snapshot first. After that, reads and writes go to Firestore.

If the local machine blocks outbound Firebase sockets, the app can keep running with local JSON because `Firebase:FallbackToJsonOnError` defaults to `true`. In that case the site will work, but the data will not be written to Firebase until network access is allowed and Firestore is reachable.
