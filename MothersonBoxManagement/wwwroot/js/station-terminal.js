(function () {
    const storageKey = "motherson.stationName";
    const hiddenInputName = "workstationName";

    function normalize(value) {
        if (!value) {
            return "";
        }

        return value.trim().replace(/\s+/g, "-").slice(0, 64);
    }

    function getStationName() {
        return normalize(localStorage.getItem(storageKey));
    }

    function setStationName(value) {
        const stationName = normalize(value);
        if (!stationName) {
            return "";
        }

        localStorage.setItem(storageKey, stationName);
        refreshStationFields();
        return stationName;
    }

    function ensureHiddenInput(form, name) {
        let input = form.querySelector(`input[name="${name}"]`);
        if (!input) {
            input = document.createElement("input");
            input.type = "hidden";
            input.name = name;
            form.appendChild(input);
        }
        return input;
    }

    function refreshStationFields() {
        const stationName = getStationName();

        document.querySelectorAll("[data-station-terminal]").forEach((element) => {
            element.textContent = stationName || "Not configured";
        });

        document.querySelectorAll(`input[name="${hiddenInputName}"]`).forEach((input) => {
            input.value = stationName;
        });

        document.querySelectorAll("form[method='post'], form[method='POST']").forEach((form) => {
            ensureHiddenInput(form, hiddenInputName).value = stationName;
        });
    }

    function appendToFormData(formData) {
        formData.set(hiddenInputName, getStationName());
    }

    document.addEventListener("DOMContentLoaded", () => {
        refreshStationFields();

        document.addEventListener("submit", (event) => {
            const form = event.target;
            if (form instanceof HTMLFormElement && form.method.toLowerCase() === "post") {
                ensureHiddenInput(form, hiddenInputName).value = getStationName();
            }
        }, true);

        const setupForm = document.getElementById("stationSetupForm");
        if (setupForm) {
            setupForm.addEventListener("submit", (event) => {
                event.preventDefault();
                const input = document.getElementById("stationSetupInput");
                const stationName = setStationName(input ? input.value : "");
                if (!stationName && input) {
                    input.focus();
                    return;
                }

                const modalElement = document.getElementById("stationSetupModal");
                if (modalElement && window.bootstrap) {
                    bootstrap.Modal.getOrCreateInstance(modalElement).hide();
                }
            });
        }
    });

    const stationApi = {
        appendToFormData,
        getStationName
    };

    // The editor is rendered only for administrators.
    if (document.getElementById("settingStation")) {
        stationApi.setStationName = setStationName;
    }

    window.MothersonStation = stationApi;
})();
