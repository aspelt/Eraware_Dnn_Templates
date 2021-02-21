/* tslint:disable */
/* eslint-disable */
//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.10.1.0 (NJsonSchema v10.3.3.0 (Newtonsoft.Json v11.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------
// ReSharper disable InconsistentNaming

import { DnnServicesFramework } from '@eraware/dnn-elements';
export class ClientBase {

  private sf: DnnServicesFramework;
  private moduleId: number;

  constructor(configuration: ConfigureRequest) {
    this.moduleId = configuration.moduleId;
    this.sf = new DnnServicesFramework(this.moduleId);
  }

  protected getBaseUrl(_defaultUrl: string, baseUrl?: string) {
    baseUrl = this.sf.getServiceRoot("Eraware_MyModule21");

    // Strips the last / if present for future concatenations
    baseUrl = baseUrl.replace(/\/$/, "");

    return baseUrl || "";
  }

  protected transformOptions(options: RequestInit): Promise<RequestInit> {
    var dnnHeaders = this.sf.getModuleHeaders();

    dnnHeaders.forEach((value, key) => {
      options.headers[key] = value;
    });

    return Promise.resolve(options);
  }
}

export class ItemClient extends ClientBase {
  private http: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };
  private baseUrl: string;
  protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

  constructor(configuration: ConfigureRequest, baseUrl?: string, http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> }) {
    super(configuration);
    this.http = http ? http : <any>window;
    this.baseUrl = this.getBaseUrl("", baseUrl);
  }

  /**
   * Creates a new item.
   * @param item (optional) The item to create.
   * @return OK
   */
  createItem(item: CreateItemDTO | null | undefined, signal?: AbortSignal | undefined): Promise<ItemViewModel> {
    let url_ = this.baseUrl + "/Item/CreateItem";
    url_ = url_.replace(/[?&]$/, "");

    const content_ = JSON.stringify(item);

    let options_ = <RequestInit>{
      body: content_,
      method: "POST",
      signal,
      headers: {
        "Content-Type": "application/json",
        "Accept": "application/json"
      }
    };

    return this.transformOptions(options_).then(transformedOptions_ => {
      return this.http.fetch(url_, transformedOptions_);
    }).then((_response: Response) => {
      return this.processCreateItem(_response);
    });
  }

  protected processCreateItem(response: Response): Promise<ItemViewModel> {
    const status = response.status;
    let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v: any, k: any) => _headers[k] = v); };
    if (status === 200) {
      return response.text().then((_responseText) => {
        let result200: any = null;
        let resultData200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
        result200 = ItemViewModel.fromJS(resultData200);
        return result200;
      });
    } else if (status === 400) {
      return response.text().then((_responseText) => {
        let result400: any = null;
        let resultData400 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
        result400 = resultData400 !== undefined ? resultData400 : <any>null;
        return throwException("Bad Request", status, _responseText, _headers, result400);
      });
    } else if (status === 500) {
      return response.text().then((_responseText) => {
        let result500: any = null;
        let resultData500 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
        result500 = Exception.fromJS(resultData500);
        return throwException("Error", status, _responseText, _headers, result500);
      });
    } else if (status !== 200 && status !== 204) {
      return response.text().then((_responseText) => {
        return throwException("An unexpected server error occurred.", status, _responseText, _headers);
      });
    }
    return Promise.resolve<ItemViewModel>(<any>null);
  }

  /**
   * Gets a paged and sorted list of items matching a certain query.
   * @param query (optional) Gets or sets the optional search query.
   * @param page (optional) Gets or sets the page number to get.
   * @param pageSize (optional) Gets or sets the size of pages.
   * @param descending (optional) Gets or sets a value indicating whether the items should be ordered descending.
   * @return OK
   */
  getItemsPage(query: string | null | undefined, page: number | undefined, pageSize: number | undefined, descending: boolean | undefined, signal?: AbortSignal | undefined): Promise<ItemsPageViewModel> {
    let url_ = this.baseUrl + "/Item/GetItemsPage?";
    if (query !== undefined && query !== null)
      url_ += "Query=" + encodeURIComponent("" + query) + "&";
    if (page === null)
      throw new Error("The parameter 'page' cannot be null.");
    else if (page !== undefined)
      url_ += "Page=" + encodeURIComponent("" + page) + "&";
    if (pageSize === null)
      throw new Error("The parameter 'pageSize' cannot be null.");
    else if (pageSize !== undefined)
      url_ += "PageSize=" + encodeURIComponent("" + pageSize) + "&";
    if (descending === null)
      throw new Error("The parameter 'descending' cannot be null.");
    else if (descending !== undefined)
      url_ += "Descending=" + encodeURIComponent("" + descending) + "&";
    url_ = url_.replace(/[?&]$/, "");

    let options_ = <RequestInit>{
      method: "GET",
      signal,
      headers: {
        "Accept": "application/json"
      }
    };

    return this.transformOptions(options_).then(transformedOptions_ => {
      return this.http.fetch(url_, transformedOptions_);
    }).then((_response: Response) => {
      return this.processGetItemsPage(_response);
    });
  }

  protected processGetItemsPage(response: Response): Promise<ItemsPageViewModel> {
    const status = response.status;
    let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v: any, k: any) => _headers[k] = v); };
    if (status === 200) {
      return response.text().then((_responseText) => {
        let result200: any = null;
        let resultData200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
        result200 = ItemsPageViewModel.fromJS(resultData200);
        return result200;
      });
    } else if (status === 500) {
      return response.text().then((_responseText) => {
        let result500: any = null;
        let resultData500 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
        result500 = Exception.fromJS(resultData500);
        return throwException("Error", status, _responseText, _headers, result500);
      });
    } else if (status !== 200 && status !== 204) {
      return response.text().then((_responseText) => {
        return throwException("An unexpected server error occurred.", status, _responseText, _headers);
      });
    }
    return Promise.resolve<ItemsPageViewModel>(<any>null);
  }

  /**
   * Deletes an existing item.
   * @param itemId The id of the item to delete.
   * @return OK
   */
  deleteItem(itemId: number, signal?: AbortSignal | undefined): Promise<void> {
    let url_ = this.baseUrl + "/Item/DeleteItem?";
    if (itemId === undefined || itemId === null)
      throw new Error("The parameter 'itemId' must be defined and cannot be null.");
    else
      url_ += "itemId=" + encodeURIComponent("" + itemId) + "&";
    url_ = url_.replace(/[?&]$/, "");

    let options_ = <RequestInit>{
      method: "POST",
      signal,
      headers: {
      }
    };

    return this.transformOptions(options_).then(transformedOptions_ => {
      return this.http.fetch(url_, transformedOptions_);
    }).then((_response: Response) => {
      return this.processDeleteItem(_response);
    });
  }

  protected processDeleteItem(response: Response): Promise<void> {
    const status = response.status;
    let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v: any, k: any) => _headers[k] = v); };
    if (status === 200) {
      return response.text().then((_responseText) => {
        return;
      });
    } else if (status === 500) {
      return response.text().then((_responseText) => {
        let result500: any = null;
        let resultData500 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
        result500 = Exception.fromJS(resultData500);
        return throwException("Error", status, _responseText, _headers, result500);
      });
    } else if (status !== 200 && status !== 204) {
      return response.text().then((_responseText) => {
        return throwException("An unexpected server error occurred.", status, _responseText, _headers);
      });
    }
    return Promise.resolve<void>(<any>null);
  }

  /**
   * Checks if a user can edit the current items.
   * @return OK
   */
  userCanEdit(signal?: AbortSignal | undefined): Promise<boolean> {
    let url_ = this.baseUrl + "/Item/UserCanEdit";
    url_ = url_.replace(/[?&]$/, "");

    let options_ = <RequestInit>{
      method: "GET",
      signal,
      headers: {
        "Accept": "application/json"
      }
    };

    return this.transformOptions(options_).then(transformedOptions_ => {
      return this.http.fetch(url_, transformedOptions_);
    }).then((_response: Response) => {
      return this.processUserCanEdit(_response);
    });
  }

  protected processUserCanEdit(response: Response): Promise<boolean> {
    const status = response.status;
    let _headers: any = {}; if (response.headers && response.headers.forEach) { response.headers.forEach((v: any, k: any) => _headers[k] = v); };
    if (status === 200) {
      return response.text().then((_responseText) => {
        let result200: any = null;
        let resultData200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
        result200 = resultData200 !== undefined ? resultData200 : <any>null;
        return result200;
      });
    } else if (status === 500) {
      return response.text().then((_responseText) => {
        let result500: any = null;
        let resultData500 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
        result500 = Exception.fromJS(resultData500);
        return throwException("Error", status, _responseText, _headers, result500);
      });
    } else if (status !== 200 && status !== 204) {
      return response.text().then((_responseText) => {
        return throwException("An unexpected server error occurred.", status, _responseText, _headers);
      });
    }
    return Promise.resolve<boolean>(<any>null);
  }
}

/** Represents the basic information about an item. */
export class ItemViewModel implements IItemViewModel {
  /** Gets or sets the id of the item. */
  id!: number;
  /** Gets or sets the name of the item. */
  name?: string | undefined;
  /** Gets or sets the item description. */
  description?: string | undefined;

  constructor(data?: IItemViewModel) {
    if (data) {
      for (var property in data) {
        if (data.hasOwnProperty(property))
          (<any>this)[property] = (<any>data)[property];
      }
    }
  }

  init(_data?: any) {
    if (_data) {
      this.id = _data["Id"];
      this.name = _data["Name"];
      this.description = _data["Description"];
    }
  }

  static fromJS(data: any): ItemViewModel {
    data = typeof data === 'object' ? data : {};
    let result = new ItemViewModel();
    result.init(data);
    return result;
  }

  toJSON(data?: any) {
    data = typeof data === 'object' ? data : {};
    data["Id"] = this.id;
    data["Name"] = this.name;
    data["Description"] = this.description;
    return data;
  }
}

/** Represents the basic information about an item. */
export interface IItemViewModel {
  /** Gets or sets the id of the item. */
  id: number;
  /** Gets or sets the name of the item. */
  name?: string | undefined;
  /** Gets or sets the item description. */
  description?: string | undefined;
}

export class Exception implements IException {
  message?: string | undefined;
  innerException?: Exception | undefined;
  stackTrace?: string | undefined;
  source?: string | undefined;

  constructor(data?: IException) {
    if (data) {
      for (var property in data) {
        if (data.hasOwnProperty(property))
          (<any>this)[property] = (<any>data)[property];
      }
    }
  }

  init(_data?: any) {
    if (_data) {
      this.message = _data["Message"];
      this.innerException = _data["InnerException"] ? Exception.fromJS(_data["InnerException"]) : <any>undefined;
      this.stackTrace = _data["StackTrace"];
      this.source = _data["Source"];
    }
  }

  static fromJS(data: any): Exception {
    data = typeof data === 'object' ? data : {};
    let result = new Exception();
    result.init(data);
    return result;
  }

  toJSON(data?: any) {
    data = typeof data === 'object' ? data : {};
    data["Message"] = this.message;
    data["InnerException"] = this.innerException ? this.innerException.toJSON() : <any>undefined;
    data["StackTrace"] = this.stackTrace;
    data["Source"] = this.source;
    return data;
  }
}

export interface IException {
  message?: string | undefined;
  innerException?: Exception | undefined;
  stackTrace?: string | undefined;
  source?: string | undefined;
}

/** Data transfer object to create a new item. */
export class CreateItemDTO implements ICreateItemDTO {
  /** Gets or sets the name for the item. */
  name?: string | undefined;
  /** Gets or sets the description of the item. */
  description?: string | undefined;

  constructor(data?: ICreateItemDTO) {
    if (data) {
      for (var property in data) {
        if (data.hasOwnProperty(property))
          (<any>this)[property] = (<any>data)[property];
      }
    }
  }

  init(_data?: any) {
    if (_data) {
      this.name = _data["Name"];
      this.description = _data["Description"];
    }
  }

  static fromJS(data: any): CreateItemDTO {
    data = typeof data === 'object' ? data : {};
    let result = new CreateItemDTO();
    result.init(data);
    return result;
  }

  toJSON(data?: any) {
    data = typeof data === 'object' ? data : {};
    data["Name"] = this.name;
    data["Description"] = this.description;
    return data;
  }
}

/** Data transfer object to create a new item. */
export interface ICreateItemDTO {
  /** Gets or sets the name for the item. */
  name?: string | undefined;
  /** Gets or sets the description of the item. */
  description?: string | undefined;
}

/** Represents a page of items, Item. */
export class ItemsPageViewModel implements IItemsPageViewModel {
  /** Gets or sets a list of items for this page. */
  items?: ItemViewModel[] | undefined;
  /** Gets or sets the current page number. */
  page!: number;
  /** Gets or sets the total amount of results found. */
  resultCount!: number;
  /** Gets or sets the total amount of pages available. */
  pageCount!: number;

  constructor(data?: IItemsPageViewModel) {
    if (data) {
      for (var property in data) {
        if (data.hasOwnProperty(property))
          (<any>this)[property] = (<any>data)[property];
      }
    }
  }

  init(_data?: any) {
    if (_data) {
      if (Array.isArray(_data["Items"])) {
        this.items = [] as any;
        for (let item of _data["Items"])
          this.items!.push(ItemViewModel.fromJS(item));
      }
      this.page = _data["Page"];
      this.resultCount = _data["ResultCount"];
      this.pageCount = _data["PageCount"];
    }
  }

  static fromJS(data: any): ItemsPageViewModel {
    data = typeof data === 'object' ? data : {};
    let result = new ItemsPageViewModel();
    result.init(data);
    return result;
  }

  toJSON(data?: any) {
    data = typeof data === 'object' ? data : {};
    if (Array.isArray(this.items)) {
      data["Items"] = [];
      for (let item of this.items)
        data["Items"].push(item.toJSON());
    }
    data["Page"] = this.page;
    data["ResultCount"] = this.resultCount;
    data["PageCount"] = this.pageCount;
    return data;
  }
}

/** Represents a page of items, Item. */
export interface IItemsPageViewModel {
  /** Gets or sets a list of items for this page. */
  items?: ItemViewModel[] | undefined;
  /** Gets or sets the current page number. */
  page: number;
  /** Gets or sets the total amount of results found. */
  resultCount: number;
  /** Gets or sets the total amount of pages available. */
  pageCount: number;
}

export class ApiException extends Error {
  message: string;
  status: number;
  response: string;
  headers: { [key: string]: any; };
  result: any;

  constructor(message: string, status: number, response: string, headers: { [key: string]: any; }, result: any) {
    super();

    this.message = message;
    this.status = status;
    this.response = response;
    this.headers = headers;
    this.result = result;
  }

  protected isApiException = true;

  static isApiException(obj: any): obj is ApiException {
    return obj.isApiException === true;
  }
}

function throwException(message: string, status: number, response: string, headers: { [key: string]: any; }, result?: any): any {
  if (result !== null && result !== undefined)
    throw result;
  else
    throw new ApiException(message, status, response, headers, null);
}

export interface ConfigureRequest {
  moduleId: number;
}
