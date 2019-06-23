import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSelectChange } from '@angular/material';
import { IdentityAttributeValidationDescriptorDto } from '../serviceProvider.service';

export interface DialogData {
  identityAttributeValidationDescriptors: IdentityAttributeValidationDescriptorDto[];
  attributeTypes: string[];
  selectedAttributeType: string;
  ageRestriction: string;
}

@Component({
  selector: 'dialog-add-validation',
  templateUrl: 'add-validation-dialog.component.html',
})
export class DialogAddValidationDialog {
  public showAgeCriterionValue: boolean;

  constructor(
    public dialogRef: MatDialogRef<DialogAddValidationDialog>,
    @Inject(MAT_DIALOG_DATA) public data: DialogData) {
    this.showAgeCriterionValue = false;
  }

  onCancelClick(): void {
    this.dialogRef.close();
  }

  onSelectionChange(evt: MatSelectChange) {
    //console.log("onSelectionChange: " + JSON.stringify(evt));
    this.data.selectedAttributeType = this.getAttributeType(evt.value);
    if (this.data.selectedAttributeType == "4") {
      this.showAgeCriterionValue = true;
    }
    else {
      this.showAgeCriterionValue = false;
    }
  }

  getAttributeType(attributeTypeName: string) {
    for (let validationDesc of this.data.identityAttributeValidationDescriptors) {
      if (validationDesc.attributeTypeName == attributeTypeName) {
        return validationDesc.attributeType;
      }
    }

    return "0";
  }
}
