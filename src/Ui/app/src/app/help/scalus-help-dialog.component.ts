import { Component, Inject,  } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
  selector: 'scalus-help-dialog',
  templateUrl: 'scalus-help-dialog.component.html',
})
export class ScalusHelpDialogComponent {

  info: string = '';

  constructor(
    public dialogRef: MatDialogRef<ScalusHelpDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data?: any) {
      var info = <string>data;
      this.info = info;
  }

  close(): void {
    this.dialogRef.close();
  }
}