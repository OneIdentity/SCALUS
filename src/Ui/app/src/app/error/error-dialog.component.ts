import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
    selector: 'error-dialog',
    templateUrl: 'error-dialog.component.html',
  })
  export class ErrorDialogComponent {
  
    error: string;
  
    constructor(
      public dialogRef: MatDialogRef<ErrorDialogComponent>,
      @Inject(MAT_DIALOG_DATA) public data?: any) {
        this.error = <string>data;
    }
  
    close(): void {
      this.dialogRef.close();
    }
  
  }