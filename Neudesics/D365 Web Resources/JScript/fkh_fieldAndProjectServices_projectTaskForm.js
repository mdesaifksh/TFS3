function OnDateChange(startDateAttribute, EndDateAttribute) {
    setCurrentTime(startDateAttribute);
    setCurrentTime(EndDateAttribute);
    var startDateVal = Xrm.Page.getAttribute(startDateAttribute).getValue();
    var startDateCtrl = Xrm.Page.getControl(startDateAttribute);
    var endDateCtrl = Xrm.Page.getControl(EndDateAttribute);
    var endDateVal = Xrm.Page.getAttribute(EndDateAttribute).getValue();

    if (startDateVal !== null && endDateVal !== null) {
        Xrm.Utility.alertDialog(String(startDateVal - endDateVal));
        if (startDateVal - endDateVal > 59999) {
            alert(startDateCtrl.getLabel() + " can not be greater than " + endDateCtrl.getLabel());
            Xrm.Page.getAttribute(startDateAttribute).setValue(null);
        } else if (startDateVal - endDateVal <= 59999){
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