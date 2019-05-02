function OnDateChange(startDateAttribute, EndDateAttribute) {
    setCurrentTime(startDateAttribute);
    setCurrentTime(EndDateAttribute);
    var startDateVal = Xrm.Page.getAttribute(startDateAttribute).getValue();
    var startDateCtrl = Xrm.Page.getControl(startDateAttribute);
    var endDateCtrl = Xrm.Page.getControl(EndDateAttribute);
    var endDateVal = Xrm.Page.getAttribute(EndDateAttribute).getValue();

    if (startDateVal !== null && endDateVal !== null) {
        var diff = startDateVal - endDateVal;
        if (diff > 59999) {
            alert(startDateCtrl.getLabel() + " can not be greater than " + endDateCtrl.getLabel());
            Xrm.Page.getAttribute(startDateAttribute).setValue(null);
        } else if (diff > 0 && diff <= 59999) {
            Xrm.Page.getAttribute(startDateAttribute).setValue(Xrm.Page.getAttribute(EndDateAttribute).getValue());
        }
    }
    else if (startDateVal === null && endDateVal !== null) {
        alert("Please set " + startDateCtrl.getLabel() + " first.");
        Xrm.Page.getAttribute(EndDateAttribute).setValue(null);
        startDateCtrl.setFocus();
    }
    //alert("Start Date " + startDateVal);
    //alert("End Date" + endDateVal);
}

function setCurrentTime(dateAttribute) {
    var dateval = Xrm.Page.getAttribute(dateAttribute).getValue();
    if (dateval !== null && dateval.getHours() == 8 && dateval.getMinutes() == 0 && dateval.getSeconds() == 0) {
        var currentDate = new Date();
        Xrm.Page.getAttribute(dateAttribute).setValue(dateval.setHours(currentDate.getHours(), currentDate.getMinutes(), 0));
    }
}

function onChange_StatusReason() {
    //Only call from Admin form.  Makes manual restart of tasks easier
    var statusReason = Xrm.Page.getAttribute("statuscode").getValue();
    if (statusReason != null) {
        if (statusReason == 963850000 /*In Progress*/) {
            if (Xrm.Page.getAttribute("msdyn_progress").getValue() == null || Xrm.Page.getAttribute("msdyn_progress").getValue() != 1) Xrm.Page.getAttribute("msdyn_progress").setValue(1);
            if (Xrm.Page.getAttribute("msdyn_actualend").getValue() != null) Xrm.Page.getAttribute("msdyn_actualend").setValue(null);
            if (Xrm.Page.getAttribute("msdyn_actualdurationminutes").getValue() != null) Xrm.Page.getAttribute("msdyn_actualdurationminutes").setValue(null);
        } else if (statusReason == 1 /*Not Started*/) {
            if (Xrm.Page.getAttribute("msdyn_progress").getValue() == null || Xrm.Page.getAttribute("msdyn_progress").getValue() != 0) Xrm.Page.getAttribute("msdyn_progress").setValue(0);
            if (Xrm.Page.getAttribute("msdyn_actualend").getValue() != null) Xrm.Page.getAttribute("msdyn_actualend").setValue(null);
            if (Xrm.Page.getAttribute("msdyn_actualstart").getValue() != null) Xrm.Page.getAttribute("msdyn_actualstart").setValue(null);
            if (Xrm.Page.getAttribute("msdyn_actualdurationminutes").getValue() != null) Xrm.Page.getAttribute("msdyn_actualdurationminutes").setValue(null);
        }
    }
}

