import { Component, Inject,  } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { TokenDisplay } from '../../api/api.service';

@Component({
  selector: 'applications-tokens-dialog',
  templateUrl: 'scalus-applications-tokens-dialog.component.html',
})
export class ScalusApplicationsTokensDialogComponent {

  tokens:TokenDisplay[];

  constructor(
    public dialogRef: MatDialogRef<ScalusApplicationsTokensDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data?: any) {
      var tokens = new Array<TokenDisplay>();
      for (let key in data)
      {
        var token = <TokenDisplay>{};
        token.name = key;
        token.description = data[key];
        tokens.push(token);
      }
      tokens.sort(function(a,b) {
        return a.name.localeCompare(b.name);
      });
      this.tokens = tokens;
  }

  close(): void {
    this.dialogRef.close();
  }
}