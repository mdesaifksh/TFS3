if (typeof (FKH) == "undefined") {
    FKH = { __namespace: true };
}
if (typeof (FKH.FieldAndProjectServices) == "undefined") {
    FKH.FieldAndProjectServices = {};
}
FKH.FieldAndProjectServices.AppointmentForm = {
    onFormLoad: function(){
        if(Xrm.Page.getAttribute("regardingobjectid") != null){
            var regardingObject = Xrm.Page.getAttribute("regardingobjectid").getValue();
            if(regardingObject != null && regardingObject[0] != null && regardingObject[0].entityType == "msdyn_projecttask"){
                FKH.FieldAndProjectServices.AppointmentForm.setRequiredAttendeesAndLocation(regardingObject[0].id);
                Xrm.Page.getAttribute("subject").setValue(regardingObject[0].name);
            }
        }
    },

    setRequiredAttendeesAndLocation: function(taskID) {
        try {
            var partyList = new Array();
            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/systemusers("+FKH.FieldAndProjectServices.AppointmentForm.removeCurlies(Xrm.Page.context.getUserId())+")?$select=fullname", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            req.onreadystatechange = function() {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        var result = JSON.parse(this.response);
                        partyList[0] = new Object();
                        partyList[0].id = Xrm.Page.context.getUserId(); 
                        partyList[0].name = result["fullname"]; 
                        partyList[0].entityType = "systemuser";
                        Xrm.Page.getAttribute("requiredattendees").setValue(partyList);
                        FKH.FieldAndProjectServices.AppointmentForm.getTask(partyList,taskID);
                    } else {
                        Xrm.Utility.alertDialog(this.response);
                    }
                }
            };
            req.send(); 
        } catch (err){
            Xrm.Utility.alertDialog(err.message);
        }
    },

    getTask: function(partyList,taskID) {
        var req = new XMLHttpRequest();
        req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/msdyn_projecttasks("+FKH.FieldAndProjectServices.AppointmentForm.removeCurlies(taskID)+")?$select=_fkh_unitid_value", true);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        req.onreadystatechange = function() {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 200) {
                    var result = JSON.parse(this.response);
                    var _fkh_unitid_value = result["_fkh_unitid_value"];
                    FKH.FieldAndProjectServices.AppointmentForm.getUnit(partyList,_fkh_unitid_value);
                } else {
                    Xrm.Utility.alertDialog(this.response);
                }
            }
        };
        req.send();
    },

    getUnit: function(partyList,unitID) {
        var req = new XMLHttpRequest();
        req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/po_units("+FKH.FieldAndProjectServices.AppointmentForm.removeCurlies(unitID)+")?$select=po_unitaddline1,po_unitaddline2,po_unitcity,po_unitstate,po_unitzip", true);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        req.onreadystatechange = function() {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 200) {
                    var result = JSON.parse(this.response);
                    var po_unitaddline1 = result["po_unitaddline1"];
                    if (result["po_unitaddline2"] != null) po_unitaddline1 += (", " + result["po_unitaddline2"]);
                    var po_unitcity = result["po_unitcity"];
                    var po_unitstate_formatted = result["po_unitstate@OData.Community.Display.V1.FormattedValue"];
                    var po_unitzip = result["po_unitzip"];
                    Xrm.Page.getAttribute("location").setValue(po_unitaddline1 + ", " + po_unitcity + ", " + po_unitstate_formatted + " " + po_unitzip);
                    try {
                        FKH.FieldAndProjectServices.AppointmentForm.getTenant(partyList,unitID);
                    } catch (err) {
                        Xrm.Utility.alertDialog(err.message);
                    }
                } else {
                    Xrm.Utility.alertDialog(this.response);
                }
            }
        };
        req.send();
    },

    getTenant: function(partyList,unitID) {
        try {
            var now = FKH.FieldAndProjectServices.AppointmentForm.formatODataDate(new Date());
            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/po_leases?$select=_po_tenantid_value&$filter=_po_unitid_value eq " + FKH.FieldAndProjectServices.AppointmentForm.removeCurlies(unitID) + " and po_leasefromdate le " + now + " and po_leaseto ge " + now, true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            req.onreadystatechange = function() {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        var results = JSON.parse(this.response);
                        for (var i = 0; i < results.value.length; i++) {
                            var _po_tenantid_value = results.value[i]["_po_tenantid_value"];
                            var _po_tenantid_value_formatted = results.value[i]["_po_tenantid_value@OData.Community.Display.V1.FormattedValue"];
                            var _po_tenantid_value_lookuplogicalname = results.value[i]["_po_tenantid_value@Microsoft.Dynamics.CRM.lookuplogicalname"];
                            partyList[1] = new Object();
                            partyList[1].id = _po_tenantid_value; 
                            partyList[1].name = _po_tenantid_value_formatted; 
                            partyList[1].entityType = _po_tenantid_value_lookuplogicalname;
                            Xrm.Page.getAttribute("requiredattendees").setValue(partyList);
                        }
                    } else {
                        Xrm.Utility.alertDialog(this.response);
                    }
                }
            };
            req.send();
        }
        catch (err) {
            Xrm.Utility.alertDialog(err.message);
        }
    },
    
    removeCurlies: function(GUID){
        return String(GUID).replace("{","").replace("}","");
    },
    
    formatODataDate: function(date){
        var d = new Date(date),
            month = '' + (d.getMonth() + 1),
            day = '' + d.getDate(),
            year = d.getFullYear();
    
        if (month.length < 2) month = '0' + month;
        if (day.length < 2) day = '0' + day;
    
        return [year, month, day].join('-');
    }
}
