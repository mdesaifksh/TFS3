function OnDateChange(startDateAttribute, EndDateAttribute) {
    var startDateVal = Xrm.Page.getAttribute(startDateAttribute).getValue();
    var startDateCtrl = Xrm.Page.getControl(startDateAttribute);
    var endDateCtrl = Xrm.Page.getControl(EndDateAttribute);
    var endDateVal = Xrm.Page.getAttribute(EndDateAttribute).getValue();

    if (startDateVal !== null && endDateVal !== null) {
        if (startDateVal > endDateVal) {
            alert(startDateCtrl.getLabel() + " can not be greater than " + endDateCtrl.getLabel());
            Xrm.Page.getAttribute(startDateAttribute).setValue(null);
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
    if (dateval !== null) {
        var currentDate = new Date();
        Xrm.Page.getAttribute(dateAttribute).setValue(dateval.setHours(currentDate.getHours(), currentDate.getMinutes(), 0));
    }
}