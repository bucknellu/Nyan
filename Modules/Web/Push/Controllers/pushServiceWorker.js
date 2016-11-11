/*
*
*  Push Notifications codelab
*  Copyright 2015 Google Inc. All rights reserved.
*
*  Licensed under the Apache License, Version 2.0 (the "License");
*  you may not use this file except in compliance with the License.
*  You may obtain a copy of the License at
*
*      https://www.apache.org/licenses/LICENSE-2.0
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" BASIS,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*  See the License for the specific language governing permissions and
*  limitations under the License
*
*/

// Version 0.1

"use strict";

console.log("Started", self);

self.addEventListener("install", function (event) {
    self.skipWaiting();
    console.log("Installed", event);
});

self.addEventListener("activate", function (event) { console.log("Activated", event); });

self.addEventListener("push", function (event) {
    console.log("Push message event", event);
    console.log("Push message arguments", arguments);

    var data = null;
    if (event.data) {
        data = event.data.json();
        console.log("Push message data", data);
    }

    event.waitUntil(
        self.registration.showNotification(data.title, data.options));
});

self.addEventListener("notificationclick", function (event) {

    // Android doesn't close the notification when you click it
    // See http://crbug.com/463146
    event.notification.close();

    var url = null;

    try { url = event.notification.data.url; } catch (e) { }

    console.log("notificationclick: url", url);

    // Check if there's already a tab open with this URL.
    // If yes: focus on the tab.
    // If no: open a tab with the URL.
    event.waitUntil(
        clients.matchAll({ type: "window" })
        .then(function (windowClients) {
            if (url) {
                console.log("WindowClients", windowClients);
                for (var i = 0; i < windowClients.length; i++) {
                    var client = windowClients[i];
                    console.log("WindowClient", client);
                    if (client.url === url && "focus" in client) {
                        return client.focus();
                    }
                }
                if (clients.openWindow) {
                    return clients.openWindow(url);
                }

            }
        })
    );
});