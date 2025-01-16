import { HttpClient, HttpParams, HttpResponse, HttpErrorResponse, HttpHeaders } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, lastValueFrom } from "rxjs";
import { environment } from "../../environments/environment.development";
import { Result } from "../models/result";
import { User } from "../models/user";


@Injectable({
  providedIn: 'root'
})
export class ApiService {

  private readonly BASE_URL = environment.apiUrl;

  private readonly USER_KEY = 'user';
  private readonly TOKEN_KEY = 'jwtToken';

  jwt: string;

  constructor(private http: HttpClient) { }

  async get<T = void>(path: string, params: any = {}, responseType = null): Promise<Result<T>> {
    const url = `${this.BASE_URL}${path}`;
    const request$ = this.http.get(url, {
      params: new HttpParams({ fromObject: params }),
      headers: this.getHeader(),
      responseType: responseType,
      observe: 'response',
    });

    return this.sendRequest<T>(request$);
  }

  async post<T = void>(path: string, body: Object = {}, contentType = null): Promise<Result<T>> {
    const url = `${this.BASE_URL}${path}`;
    const request$ = this.http.post(url, body, {
      headers: this.getHeader(contentType),
      observe: 'response'
    });

    return this.sendRequest<T>(request$);
  }

  async put<T = void>(path: string, body: Object = {}, contentType = null): Promise<Result<T>> {
    const url = `${this.BASE_URL}${path}`;
    const request$ = this.http.put(url, body, {
      headers: this.getHeader(contentType),
      observe: 'response'
    });

    return this.sendRequest<T>(request$);
  }

  async delete<T = void>(path: string, params: any = {}): Promise<Result<T>> {
    const url = `${this.BASE_URL}${path}`;
    const request$ = this.http.delete(url, {
      params: new HttpParams({ fromObject: params }),
      headers: this.getHeader(),
      observe: 'response'
    });

    return this.sendRequest<T>(request$);
  }

  private async sendRequest<T = void>(request$: Observable<HttpResponse<any>>): Promise<Result<T>> {
    let result: Result<T>;

    try {
      const response = await lastValueFrom(request$);
      const statusCode = response.status;

      if (response.ok) {
        const data = response.body as T;

        if (data == undefined) {
          result = Result.success(statusCode);
        } else {
          result = Result.success(statusCode, data);
        }
      } else {
        result = result = Result.error(statusCode, response.statusText);
      }
    } catch (exception) {
      if (exception instanceof HttpErrorResponse) {
        result = Result.error(exception.status, exception.statusText);
      } else {
        result = Result.error(-1, exception.message);
      }
    }

    return result;
  }

  private getHeader(accept = null, contentType = null): HttpHeaders {
    let header: any = { 'Authorization': `Bearer ${this.jwt}` };

    if (accept)
      header['Accept'] = accept;

    if (contentType)
      header['Content-Type'] = contentType;

    return new HttpHeaders(header);
  }

  async getUser(id: number): Promise<User> {
    const request: Observable<Object> =
      this.http.get(`${this.BASE_URL}User/${id}`);

    const dataRaw: any = await lastValueFrom(request);

    const user: User = {
      userId: id,
      nickname: dataRaw.nickname,
      email: dataRaw.email,
      avatar: dataRaw.avatar,
      role: dataRaw.role

    };
    return user;
  }

}